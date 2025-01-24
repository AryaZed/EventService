using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Caching;
using EventService.Application.Interfaces.Services.Events;
using EventService.Application.Interfaces.Services.Integrations;
using EventService.Application.Interfaces.Services.Notifications;
using EventService.Application.Models.ML;
using EventService.Domain.Entities.Analytics;
using EventService.Domain.Entities.Events;
using EventService.Domain.Entities.Users;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
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

    public async Task ProcessRecurringEventsAsync()
    {
        var now = DateTime.UtcNow;
        var recurringEvents = await _eventRepository.GetRecurringEventsAsync(now);

        foreach (var eventEntity in recurringEvents)
        {
            var nextOccurrence = eventEntity.GetNextOccurrence();
            if (nextOccurrence is null) continue;

            var newEvent = Domain.Entities.Events.Event.Create(
                eventEntity.Title,
                eventEntity.Description,
                eventEntity.ScheduledAt,
                eventEntity.Business,
                eventEntity.TargetRulesJson,
                eventEntity.Recurrence
            );

            await _eventRepository.AddAsync(newEvent);
            _logger.LogInformation("📅 Created next occurrence for event {EventId}: {NextDate}", eventEntity.Id, nextOccurrence);
        }
    }

    public async Task<DateTime> PredictOptimalEventTime(Guid businessId)
    {
        var pastEvents = await _analyticsRepository.GetPastEventAnalyticsAsync(businessId);
        if (!pastEvents.Any()) return DateTime.UtcNow.AddHours(1); // Default if no data

        // ✅ Extract the hour from the Timestamp field (when events were processed)
        var peakHour = pastEvents
            .GroupBy(e => e.Timestamp.Hour) // ✅ Group by hour of the day
            .Select(group => new
            {
                Hour = group.Key, // ✅ This is the actual hour (0-23)
                AverageEngagement = group.Average(e => e.EngagementScore)
            })
            .OrderByDescending(e => e.AverageEngagement)
            .FirstOrDefault()?.Hour;

        return peakHour.HasValue
            ? DateTime.UtcNow.Date.AddHours(peakHour.Value)  // ✅ Corrected to use Hour
            : DateTime.UtcNow.AddHours(1);  // Default fallback
    }

    public async Task TrainEventPredictionModel()
    {
        var allEventData = await _analyticsRepository.GetAllEventAnalyticsAsync();
        if (!allEventData.Any()) return;

        var trainingData = allEventData.Select(e => new EventPredictionData
        {
            ProcessedUsers = e.ProcessedUsers,
            SuccessCount = e.SuccessCount,
            FailureCount = e.FailureCount,
            ProcessingDuration = (float)e.ProcessingDuration.TotalSeconds,
            EngagementScore = (float)e.EngagementScore
        }).ToList();

        var model = TrainModel(trainingData);
        SaveModel(model);
    }

    private ITransformer TrainModel(List<EventPredictionData> trainingData)
    {
        var mlContext = new MLContext();

        var dataView = mlContext.Data.LoadFromEnumerable(trainingData);

        var pipeline = mlContext.Transforms.Concatenate("Features", nameof(EventPredictionData.ProcessedUsers),
                nameof(EventPredictionData.SuccessCount), nameof(EventPredictionData.FailureCount),
                nameof(EventPredictionData.ProcessingDuration))
            .Append(mlContext.Regression.Trainers.FastTree(labelColumnName: "EngagementScore"));

        var model = pipeline.Fit(dataView);
        return model;
    }

    // ✅ Define Save Model Function
    private void SaveModel(ITransformer model)
    {
        var mlContext = new MLContext();
        string modelPath = "event_prediction_model.zip";
        mlContext.Model.Save(model, null, modelPath);
    }

    private async Task<DateTime> PredictBestNotificationTime(Guid userId)
    {
        var userEngagement = await _analyticsRepository.GetUserEngagementHistoryAsync(userId);
        if (!userEngagement.Any()) return DateTime.UtcNow;

        return userEngagement.OrderByDescending(e => e.EngagementScore).First().Timestamp;
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
        var filteredUsers = new HashSet<User>(); // ✅ Prevents duplicates

        // ✅ If sending to all users, fetch them directly
        if (rules.SendToAllUsers)
        {
            return await _userRepository.GetAllByBusinessIdAsync(eventEntity.BusinessId);
        }

        // ✅ Filter users who joined in the last X days
        if (rules.UserJoinedInLastDays.HasValue)
        {
            var minDate = DateTime.UtcNow.AddDays(-rules.UserJoinedInLastDays.Value);
            var newUsers = await _userRepository.GetUsersJoinedAfterAsync(eventEntity.BusinessId, minDate);
            foreach (var user in newUsers) filteredUsers.Add(user);
        }

        // ✅ Filter specific users by ID
        if (rules.SpecificUserIds is not null && rules.SpecificUserIds.Any())
        {
            var specificUsers = await _userRepository.GetUsersByIdsAsync(rules.SpecificUserIds);
            foreach (var user in specificUsers) filteredUsers.Add(user);
        }

        // ✅ Filter users based on targeted groups
        if (rules.TargetUserGroups is not null && rules.TargetUserGroups.Any())
        {
            var groupUsers = await _userRepository.GetUsersByGroupIdsAsync(rules.TargetUserGroups);
            foreach (var user in groupUsers) filteredUsers.Add(user);
        }

        // ✅ Get all business users for engagement scoring (if needed)
        var users = filteredUsers.Any()
            ? filteredUsers.ToList() // Use only filtered users
            : await _userRepository.GetAllByBusinessIdAsync(eventEntity.BusinessId); // Default fallback

        // ✅ Get engagement scores
        var engagedUsers = await _analyticsRepository.GetUserEngagementScoresAsync(eventEntity.BusinessId);

        // ✅ Sort users by engagement score (descending)
        var sortedUsers = users.OrderByDescending(u => engagedUsers.ContainsKey(u.Id) ? engagedUsers[u.Id] : 0).ToList();

        return sortedUsers;
    }
}