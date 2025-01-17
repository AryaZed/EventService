using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Notifications;
using EventService.Domain.Entities.Users;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

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

        var users = await GetTargetUsers(eventEntity);

        if (!users.Any())
        {
            _logger.LogWarning("No users found for business {BusinessId}, skipping event {EventId}", eventEntity.BusinessId, eventEntity.Id);
            return;
        }

        var tasks = users.Select(user => _notificationService.SendSmsAsync(user.PhoneNumber, $"📢 {eventEntity.Title}: {eventEntity.Description}"));
        await Task.WhenAll(tasks);

        _logger.LogInformation("✅ Successfully processed event {EventId} and notified {UserCount} users.", eventEntity.Id, users.Count);
    }

    private async Task<List<User>> GetTargetUsers(Domain.Entities.Events.Event eventEntity)
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