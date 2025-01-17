using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Notifications;
using EventService.Domain.Entities.Events;
using EventService.Domain.Entities.Users;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EventService.Application.Services.Events;

public class EventProcessor : IEventProcessor
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<EventProcessor> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public EventProcessor(
        IEventRepository eventRepository,
        IUserRepository userRepository,
        INotificationService notificationService,
        ILogger<EventProcessor> logger,
        IPublishEndpoint publishEndpoint)
    {
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task ProcessScheduledEventsAsync()
    {
        var now = DateTime.UtcNow;
        var eventsToProcess = await _eventRepository.GetScheduledEventsAsync(now);

        foreach (var eventEntity in eventsToProcess)
        {
            try
            {
                await _publishEndpoint.Publish(eventEntity);
                _logger.LogInformation("✅ Published event {EventId} to RabbitMQ", eventEntity.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to publish event {EventId}", eventEntity.Id);
            }
        }
    }  
}