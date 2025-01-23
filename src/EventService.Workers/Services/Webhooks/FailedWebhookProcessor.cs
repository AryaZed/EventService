using EventService.Application.Interfaces.Services.Caching;
using EventService.Application.Interfaces.Services.Integrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventService.Workers.Services.Webhooks
{
    public class FailedWebhookProcessor : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FailedWebhookProcessor> _logger;

        public FailedWebhookProcessor(IServiceScopeFactory scopeFactory, ILogger<FailedWebhookProcessor> logger)
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
                var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();

                var failedWebhookKeys = await cacheService.GetKeysAsync("dlq:webhooks:*");

                foreach (var key in failedWebhookKeys)
                {
                    var webhookPayload = await cacheService.GetAsync<string>(key);
                    if (string.IsNullOrEmpty(webhookPayload)) continue;

                    try
                    {
                        var webhook = JsonSerializer.Deserialize<WebhookRetryPayload>(webhookPayload);
                        if (webhook == null) continue;

                        var isSuccess = await webhookService.SendWebhookAsync(webhook.WebhookId, webhook.Payload);
                        if (isSuccess)
                        {
                            await cacheService.RemoveAsync(key);
                            _logger.LogInformation("✅ Successfully retried Webhook {WebhookId}", webhook.WebhookId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Failed to retry Webhook {WebhookId}", key);
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    public class WebhookRetryPayload
    {
        public Guid WebhookId { get; set; }
        public object Payload { get; set; }
    }
}
