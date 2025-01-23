using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Caching;
using EventService.Application.Interfaces.Services.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventService.Workers.Services.Webhooks
{
    public class WebhookFailureMonitorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<WebhookFailureMonitorService> _logger;
        private static readonly TimeSpan MonitorInterval = TimeSpan.FromMinutes(10);

        public WebhookFailureMonitorService(IServiceScopeFactory scopeFactory, ILogger<WebhookFailureMonitorService> logger)
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
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                _logger.LogInformation("🔍 Checking webhook failure counts...");

                var failedWebhookKeys = await cacheService.GetKeysAsync("failures:webhook:*");

                foreach (var key in failedWebhookKeys)
                {
                    var failureCount = await cacheService.GetAsync<int>(key);
                    if (failureCount >= 5) // Threshold: 5 failed attempts
                    {
                        var webhookId = key.Split(':').Last();
                        var webhook = await webhookRepository.GetByIdAsync(Guid.Parse(webhookId));
                        if (webhook != null)
                        {
                            var alertMessage = $"🚨 Alert! Webhook {webhook.Url} has failed {failureCount} times.";
                            await notificationService.SendSmsAsync(webhook.Business.ContactEmail, alertMessage);
                            await cacheService.RemoveAsync(key); // Reset failure counter after alert
                        }
                    }
                }

                await Task.Delay(MonitorInterval, stoppingToken);
            }
        }
    }
}
