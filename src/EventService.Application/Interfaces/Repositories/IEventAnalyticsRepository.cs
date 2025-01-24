using EventService.Domain.Entities.Analytics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventService.Application.Interfaces.Repositories
{
    public interface IEventAnalyticsRepository
    {
        Task AddAsync(EventAnalytics analytics);
        Task<List<EventAnalytics>> GetByEventIdAsync(Guid eventId);
        Task<List<EventAnalytics>> GetAllAsync();

        // ✅ Get Past Analytics for AI Predictions
        Task<List<EventAnalytics>> GetPastEventAnalyticsAsync(Guid businessId);

        // ✅ Get User Engagement Data
        Task<List<EventAnalytics>> GetUserEngagementHistoryAsync(Guid userId);

        // ✅ Get Event Engagement Scores for AI Optimization
        Task<Dictionary<Guid, double>> GetUserEngagementScoresAsync(Guid businessId);

        // ✅ Get All Analytics for AI Training
        Task<List<EventAnalytics>> GetAllEventAnalyticsAsync();
    }
}
