using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Caching;
using EventService.Domain.Entities.Integrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace EventService.Workers.Services.Webhooks
{
    public class WebhookRetryService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<WebhookRetryService> _logger;

        public WebhookRetryService(IServiceScopeFactory scopeFactory, ILogger<WebhookRetryService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
                var webhookRepository = scope.ServiceProvider.GetRequiredService<IWebhookRepository>();
                var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

                var failedWebhooks = await cacheService.GetKeysAsync("dlq:webhooks:*"); // 🔹 Dead Letter Queue (DLQ)

                foreach (var key in failedWebhooks)
                {
                    var webhookPayload = await cacheService.GetAsync<WebhookPayload>(key);
                    if (webhookPayload is not null)
                    {
                        var webhook = await webhookRepository.GetByIdAsync(webhookPayload.WebhookId);
                        if (webhook is null) continue;

                        bool success = await ResendWebhookAsync(httpClientFactory, webhook, webhookPayload);
                        if (success)
                        {
                            await cacheService.RemoveAsync(key); // ✅ Remove after successful retry
                            _logger.LogInformation("✅ Webhook successfully retried: {WebhookId}", webhook.Id);
                        }
                        else
                        {
                            _logger.LogWarning("❌ Webhook retry failed: {WebhookId}, will retry later", webhook.Id);
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // 🔹 Retry every 5 minutes
            }
        }

        private async Task<bool> ResendWebhookAsync(IHttpClientFactory httpClientFactory, Webhook webhook, WebhookPayload payload)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient();
                var response = await httpClient.PostAsJsonAsync(webhook.Url, payload);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook retry failed for {WebhookId}", webhook.Id);
                return false;
            }
        }
    }

    public record WebhookPayload(Guid WebhookId, string EventType, object Data);
}
