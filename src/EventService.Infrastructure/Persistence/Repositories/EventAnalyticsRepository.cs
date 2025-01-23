using EventService.Application.Interfaces.Repositories;
using EventService.Domain.Entities.Analytics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
