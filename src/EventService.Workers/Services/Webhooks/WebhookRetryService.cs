using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Caching;
using EventService.Domain.Entities.Integrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventService.Workers.Services.Webhooks;

public class WebhookRetryService : BackgroundService
{
    private readonly ICacheService _cacheService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookRetryService> _logger;

    private static readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy = Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .Or<HttpRequestException>()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            (result, timeSpan, retryCount, context) =>
            {
                Console.WriteLine($"🔄 Retrying Webhook (Attempt {retryCount}) after {timeSpan.TotalSeconds}s...");
            });

    private static readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreakerPolicy = Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .Or<HttpRequestException>()
        .CircuitBreakerAsync(3, TimeSpan.FromMinutes(2),
            onBreak: (result, duration) =>
            {
                Console.WriteLine($"🚨 Circuit breaker triggered! Blocking webhook retries for {duration.TotalSeconds}s.");
            },
            onReset: () =>
            {
                Console.WriteLine("✅ Circuit breaker reset! Resuming normal operation.");
            });

    public WebhookRetryService(
        ICacheService cacheService,
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<WebhookRetryService> logger)
    {
        _cacheService = cacheService;
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("🔍 Checking failed webhooks...");

                var failedWebhooks = await _cacheService.GetKeysAsync("dlq:webhooks:*");

                foreach (var key in failedWebhooks)
                {
                    if (stoppingToken.IsCancellationRequested) break; // ✅ Stop immediately if cancellation is requested

                    var webhookPayloadJson = await _cacheService.GetAsync<string>(key);
                    if (string.IsNullOrEmpty(webhookPayloadJson)) continue;

                    try
                    {
                        var webhookPayload = JsonSerializer.Deserialize<WebhookPayload>(webhookPayloadJson);
                        if (webhookPayload is null) continue;

                        using var scope = _scopeFactory.CreateScope();
                        var webhookRepository = scope.ServiceProvider.GetRequiredService<IWebhookRepository>();

                        var webhook = await webhookRepository.GetByIdAsync(webhookPayload.WebhookId);
                        if (webhook is null)
                        {
                            _logger.LogWarning("⚠️ Webhook {WebhookId} not found in database, removing from DLQ", webhookPayload.WebhookId);
                            await _cacheService.RemoveAsync(key);
                            continue;
                        }

                        bool success = await ResendWebhookAsync(webhook, webhookPayload, stoppingToken);
                        if (success)
                        {
                            await _cacheService.RemoveAsync(key);
                            _logger.LogInformation("✅ Webhook successfully retried: {WebhookId}", webhook.Id);
                        }
                        else
                        {
                            _logger.LogWarning("❌ Webhook retry failed: {WebhookId}, will retry later", webhook.Id);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        _logger.LogWarning("⚠️ Webhook retry task was canceled.");
                        return; // ✅ Ensure graceful exit on shutdown
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Failed to retry Webhook {WebhookId}", key);
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("⚠️ Webhook Retry Service is stopping.");
                break; // ✅ Exit loop cleanly
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error in Webhook Retry Service.");
            }
        }
    }

    private async Task<bool> ResendWebhookAsync(Webhook webhook, WebhookPayload payload, CancellationToken stoppingToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
            {
                Content = JsonContent.Create(payload)
            };

            request.Headers.Add("X-Webhook-Id", webhook.Id.ToString());

            var response = await _retryPolicy.ExecuteAsync(async () =>
                await _circuitBreakerPolicy.ExecuteAsync(() => httpClient.SendAsync(request, stoppingToken)));

            return response.IsSuccessStatusCode;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("⚠️ Webhook request was canceled for {WebhookId}", webhook.Id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook retry failed for {WebhookId}", webhook.Id);
            return false;
        }
    }
}

public record WebhookPayload(Guid WebhookId, string EventType, object Data);
