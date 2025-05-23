﻿namespace EventService.Domain.Entities.Analytics;

public class EventAnalytics
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid EventId { get; private set; }
    public int ProcessedUsers { get; private set; }
    public int SuccessCount { get; private set; }
    public int FailureCount { get; private set; }
    public TimeSpan ProcessingDuration { get; private set; }
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
    public double EngagementScore { get; private set; }

    public Events.Event Event { get; private set; }

    private EventAnalytics() { }

    public EventAnalytics(Guid eventId, int processedUsers, int successCount, int failureCount, TimeSpan processingDuration)
    {
        EventId = eventId;
        ProcessedUsers = processedUsers;
        SuccessCount = successCount;
        FailureCount = failureCount;
        ProcessingDuration = processingDuration;
        EngagementScore = CalculateEngagementScore();
    }

    private double CalculateEngagementScore()
    {
        return (double)SuccessCount / ProcessedUsers * 100; // Engagement Percentage
    }
}
