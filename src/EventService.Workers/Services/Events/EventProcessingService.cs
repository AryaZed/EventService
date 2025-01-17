using EventService.Application.Services.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventService.Workers.Services.Events;

public class EventProcessingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventProcessingService> _logger;

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
                using var scope = _serviceProvider.CreateScope();
                var eventProcessor = scope.ServiceProvider.GetRequiredService<IEventProcessor>();
                await eventProcessor.ProcessScheduledEventsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing events.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Run every minute
        }
    }
}
