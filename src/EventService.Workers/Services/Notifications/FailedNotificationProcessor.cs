using EventService.Application.Interfaces.Services.Caching;
using EventService.Application.Interfaces.Services.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventService.Workers.Services.Notifications
{
    public class FailedNotificationProcessor : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FailedNotificationProcessor> _logger;

        public FailedNotificationProcessor(IServiceScopeFactory scopeFactory, ILogger<FailedNotificationProcessor> logger)
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
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                var failedNotifications = await cacheService.GetKeysAsync("dlq:sms:*");

                foreach (var key in failedNotifications)
                {
                    var notification = await cacheService.GetAsync<JsonElement>(key);
                    if (notification.ValueKind != JsonValueKind.Undefined)
                    {
                        try
                        {
                            var phoneNumber = notification.GetProperty("PhoneNumber").GetString();
                            var message = notification.GetProperty("Message").GetString();

                            if (!string.IsNullOrEmpty(phoneNumber) && !string.IsNullOrEmpty(message))
                            {
                                await notificationService.SendSmsAsync(phoneNumber, message);
                                await cacheService.RemoveAsync(key);
                                _logger.LogInformation("✅ Retried and Sent SMS to {PhoneNumber}", phoneNumber);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Retrying failed for {PhoneNumber}, keeping in DLQ", key);
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
