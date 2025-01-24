using System;
using System.Threading;
using System.Threading.Tasks;
using EventService.Application.Interfaces.Services.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace EventService.Workers.Services.Events
{
    public class EventProcessingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventProcessingService> _logger;

        private static readonly AsyncRetryPolicy RetryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential Backoff: 2, 4, 8s
                (exception, timeSpan, retryCount, context) =>
                {
                    context.GetLogger<EventProcessingService>().LogWarning(
                        "⚠️ Retry {RetryCount} after {Delay}s due to: {ErrorMessage}",
                        retryCount, timeSpan.TotalSeconds, exception.Message
                    );
                });

        private static readonly AsyncCircuitBreakerPolicy CircuitBreakerPolicy = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                5, TimeSpan.FromMinutes(1), // Break after 5 failures, Reset after 1 min
                (exception, duration, context) =>
                {
                    context.GetLogger<EventProcessingService>().LogError(
                        "⛔ Circuit Breaker Opened! Blocking calls for {Duration}s due to: {ErrorMessage}",
                        duration.TotalSeconds, exception.Message
                    );
                },
                context =>
                {
                    context.GetLogger<EventProcessingService>().LogInformation("✅ Circuit Breaker Reset: Resuming normal operation.");
                });

        public EventProcessingService(IServiceProvider serviceProvider, ILogger<EventProcessingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("📌 Event Processing Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CircuitBreakerPolicy.ExecuteAsync(async () =>
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var eventProcessor = scope.ServiceProvider.GetRequiredService<IEventProcessor>();

                        // ✅ RetryPolicy runs inside CircuitBreaker
                        await RetryPolicy.ExecuteAsync(() => eventProcessor.ProcessScheduledEventsAsync());
                    });
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning("🛑 EventProcessingService canceled.");
                    break; // Graceful exit on shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "🚨 Unhandled error occurred while processing events.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning("🛑 Delay interrupted: EventProcessingService stopping.");
                    break;
                }
            }

            _logger.LogInformation("🛑 EventProcessingService stopped.");
        }
    }

    // ✅ Helper Extension to Pass Logger Context to Polly Policies
    public static class PollyContextLoggerExtensions
    {
        private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        public static ILogger GetLogger<T>(this Context context)
        {
            return context.TryGetValue("ILogger", out var logger) && logger is ILogger log
                ? log
                : LoggerFactory.CreateLogger<T>();
        }
    }
}
