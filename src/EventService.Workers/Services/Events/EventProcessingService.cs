using System;
using System.Threading;
using System.Threading.Tasks;
using EventService.Application.Interfaces.Services;
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
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds} seconds due to {exception.Message}");
                });

        private static readonly AsyncCircuitBreakerPolicy CircuitBreakerPolicy = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(5, TimeSpan.FromMinutes(1),
                (exception, duration) =>
                {
                    Console.WriteLine($"Circuit broken! Blocking calls for {duration.TotalSeconds} seconds.");
                },
                () =>
                {
                    Console.WriteLine("Circuit reset, resuming normal operation.");
                });

        public EventProcessingService(IServiceProvider serviceProvider, ILogger<EventProcessingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Event Processing Service is starting.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CircuitBreakerPolicy.ExecuteAsync(async () =>
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var eventProcessor = scope.ServiceProvider.GetRequiredService<IEventProcessor>();
                        await RetryPolicy.ExecuteAsync(() => eventProcessor.ProcessScheduledEventsAsync());
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing events.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Run every minute
            }
        }
    }
}
