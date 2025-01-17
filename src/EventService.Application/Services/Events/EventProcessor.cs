using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Notifications;
using EventService.Domain.Entities.Events;
using EventService.Domain.Entities.Users;
using Microsoft.Extensions.Logging;

namespace EventService.Application.Services.Events;

public class EventProcessor : IEventProcessor
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<EventProcessor> _logger;

    public EventProcessor(
        IEventRepository eventRepository,
        IUserRepository userRepository,
        INotificationService notificationService,
        ILogger<EventProcessor> logger)
    {
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ProcessScheduledEventsAsync()
    {
        var now = DateTime.UtcNow;
        var eventsToProcess = await _eventRepository.GetScheduledEventsAsync(now);

        foreach (var eventEntity in eventsToProcess)
        {
            var users = await GetTargetUsers(eventEntity);
            _logger.LogInformation("Processing event {EventId} for {UserCount} users", eventEntity.Id, users.Count);

            foreach (var user in users)
            {
                string message = $"📢 Event Alert: {eventEntity.Title} - {eventEntity.Description}";
                await _notificationService.SendSmsAsync(user.PhoneNumber, message);
            }
        }
    }

    private async Task<List<User>> GetTargetUsers(Event eventEntity)
    {
        var rules = eventEntity.GetTargetRules();

        if (rules.SendToAllUsers)
        {
            return await _userRepository.GetAllByBusinessIdAsync(eventEntity.BusinessId);
        }

        var filteredUsers = new List<User>();

        if (rules.UserJoinedInLastDays.HasValue)
        {
            var minDate = DateTime.UtcNow.AddDays(-rules.UserJoinedInLastDays.Value);
            var newUsers = await _userRepository.GetUsersJoinedAfterAsync(eventEntity.BusinessId, minDate);
            filteredUsers.AddRange(newUsers);
        }

        if (rules.SpecificUserIds is not null)
        {
            var specificUsers = await _userRepository.GetUsersByIdsAsync(rules.SpecificUserIds);
            filteredUsers.AddRange(specificUsers);
        }

        if (rules.TargetUserGroups is not null)
        {
            var groupUsers = await _userRepository.GetUsersByGroupIdsAsync(rules.TargetUserGroups);
            filteredUsers.AddRange(groupUsers);
        }

        return filteredUsers.Distinct().ToList();
    }
}