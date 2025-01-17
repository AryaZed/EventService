using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Notifications;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EventService.Infrastructure.Consumers.Events;

public class EventConsumer : IConsumer<EventService.Domain.Entities.Events.Event>
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<EventConsumer> _logger;

    public EventConsumer(IUserRepository userRepository, INotificationService notificationService, ILogger<EventConsumer> logger)
    {
        _userRepository = userRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EventService.Domain.Entities.Events.Event> context)
    {
        var eventEntity = context.Message;
        _logger.LogInformation("Received Event: {EventId} - Processing...", eventEntity.Id);

        var users = await _userRepository.GetAllByBusinessIdAsync(eventEntity.BusinessId);

        if (!users.Any())
        {
            _logger.LogWarning("No users found for business {BusinessId}, skipping event {EventId}", eventEntity.BusinessId, eventEntity.Id);
            return;
        }

        var tasks = users.Select(user => _notificationService.SendSmsAsync(user.PhoneNumber, $"📢 {eventEntity.Title}: {eventEntity.Description}"));
        await Task.WhenAll(tasks);

        _logger.LogInformation("✅ Successfully processed event {EventId} and notified {UserCount} users.", eventEntity.Id, users.Count);
    }
}