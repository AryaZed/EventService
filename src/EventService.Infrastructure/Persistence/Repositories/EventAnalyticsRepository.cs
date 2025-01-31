using EventService.Application.Interfaces.Repositories;
using EventService.Domain.Entities.Analytics;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure.Persistence.Repositories
{
    public class EventAnalyticsRepository : IEventAnalyticsRepository
    {
        private readonly ApplicationDbContext _context;

        public EventAnalyticsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(EventAnalytics analytics)
        {
            await _context.EventAnalytics.AddAsync(analytics);
            await _context.SaveChangesAsync();
        }

        public async Task<List<EventAnalytics>> GetByEventIdAsync(Guid eventId)
        {
            return await _context.EventAnalytics
                .Where(a => a.EventId == eventId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }

        public async Task<List<EventAnalytics>> GetAllAsync()
        {
            return await _context.EventAnalytics.OrderByDescending(a => a.Timestamp).ToListAsync();
        }

        public async Task<List<EventAnalytics>> GetPastEventAnalyticsAsync(Guid businessId)
        {
            return await _context.EventAnalytics
                .Where(a => a.Event.BusinessId == businessId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }

        public async Task<Dictionary<Guid, double>> GetUserEngagementScoresAsync(Guid businessId)
        {
            return await _context.EventAnalytics
                .Where(a => a.Event.BusinessId == businessId)
                .GroupBy(a => a.EventId)
                .Select(g => new { EventId = g.Key, Score = g.Average(a => a.EngagementScore) })
                .ToDictionaryAsync(e => e.EventId, e => e.Score);
        }

        // ✅ Get all event analytics (used for AI predictions)
        public async Task<List<EventAnalytics>> GetAllEventAnalyticsAsync()
        {
            return await _context.EventAnalytics
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }

        // ✅ Get user engagement history based on past event interactions
        public async Task<List<EventAnalytics>> GetUserEngagementHistoryAsync(Guid userId)
        {
            return await _context.EventAnalytics
                .Where(a => a.Event.EventAttendees.Any(ua => ua.UserId == userId)) // ✅ Tracks user participation in events
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }

    }
}
