using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Caching;
using EventService.Application.Interfaces.Services.Events;
using EventService.Application.Interfaces.Services.Integrations;
using EventService.Application.Interfaces.Services.Notifications;
using EventService.Domain.Entities.Analytics;
using EventService.Domain.Entities.Events;
using EventService.Domain.Entities.Users;
using MassTransit;
using Microsoft.Extensions.Logging;
using Polly;
using System.Diagnostics;

namespace EventService.Application.Services.Events;

public class EventProcessor : IEventProcessor
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEventAnalyticsRepository _analyticsRepository;
    private readonly ILogger<EventProcessor> _logger;
    private readonly INotificationService _notificationService;
    private readonly ICacheService _cacheService;
    private readonly IWebhookService _webhookService;

    public EventProcessor(IEventRepository eventRepository, IUserRepository userRepository,
        IEventAnalyticsRepository analyticsRepository, ILogger<EventProcessor> logger, INotificationService notificationService, ICacheService cacheService, IWebhookService webhookService)
    {
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _analyticsRepository = analyticsRepository;
        _logger = logger;
        _notificationService = notificationService;
        _cacheService = cacheService;
        _webhookService = webhookService;
    }

    public async Task ProcessScheduledEventsAsync()
    {
        var now = DateTime.UtcNow;
        var eventsToProcess = await _eventRepository.GetScheduledEventsAsync(now);

        foreach (var eventEntity in eventsToProcess)
        {
            var stopwatch = Stopwatch.StartNew();

            await _webhookService.TriggerWebhooksAsync(eventEntity);

            var users = await GetTargetUsers(eventEntity);

            int successCount = 0;
            int failureCount = 0;

            foreach (var user in users)
            {
                bool success = await SendNotificationAsync(user, eventEntity);
                if (success)
                    successCount++;
                else
                    failureCount++;
            }

            stopwatch.Stop();

            var analytics = new EventAnalytics(
                eventEntity.Id,
                users.Count,
                successCount,
                failureCount,
                stopwatch.Elapsed
            );

            await _analyticsRepository.AddAsync(analytics);

            _logger.LogInformation("Processed event {EventId} - Success: {SuccessCount}, Failure: {FailureCount}, Duration: {Duration}",
                eventEntity.Id, successCount, failureCount, stopwatch.Elapsed);
        }
    }

    private async Task<bool> SendNotificationAsync(User user, Domain.Entities.Events.Event eventEntity)
    {
        var message = $"🔔 Event Reminder: {eventEntity.Title} is scheduled for {eventEntity.ScheduledAt:yyyy-MM-dd HH:mm}.";

        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential Backoff
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning("⚠️ Retrying ({RetryCount}/3) SMS to {PhoneNumber} due to error: {Error}", retryCount, user.PhoneNumber, exception.Message);
                });

        try
        {
            await retryPolicy.ExecuteAsync(async () => await _notificationService.SendSmsAsync(user.PhoneNumber, message));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ SMS delivery failed after retries for {PhoneNumber} and Event {EventId}. Moving to Dead Letter Queue.", user.PhoneNumber, eventEntity.Id);
            await MoveToDeadLetterQueue(user.PhoneNumber, message);
            return false;
        }
    }

    private async Task MoveToDeadLetterQueue(string phoneNumber, string message)
    {
        var failedNotification = new
        {
            PhoneNumber = phoneNumber,
            Message = message,
            FailedAt = DateTime.UtcNow
        };

        string cacheKey = $"dlq:sms:{Guid.NewGuid()}";
        await _cacheService.SetAsync(cacheKey, failedNotification, TimeSpan.FromDays(7)); // Keep for 7 days

        _logger.LogWarning("🚨 Moved failed SMS to Dead Letter Queue: {PhoneNumber} - {Message}", phoneNumber, message);
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