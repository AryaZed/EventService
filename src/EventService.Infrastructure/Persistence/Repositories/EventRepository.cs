using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Caching;
using EventService.Domain.Entities.Events;
using Microsoft.EntityFrameworkCore;

namespace EventService.Infrastructure.Persistence.Repositories
{
    public class EventRepository : IEventRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;

        public EventRepository(ApplicationDbContext context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        public async Task<IEnumerable<Event>> GetAllAsync() =>
            await _context.Events.Include(e => e.Business).ToListAsync();

        public async Task<Event?> GetByIdAsync(Guid eventId)
        {
            var cacheKey = $"event:{eventId}";
            var cachedEvent = await _cacheService.GetAsync<Event>(cacheKey);
            if (cachedEvent is not null)
                return cachedEvent;

            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity is not null)
            {
                await _cacheService.SetAsync(cacheKey, eventEntity, TimeSpan.FromMinutes(30)); // ✅ Cache for 30 min
            }

            return eventEntity;
        }

        public async Task AddAsync(Event eventEntity)
        {
            await _context.Events.AddAsync(eventEntity);
            await _context.SaveChangesAsync();
            await _cacheService.RemoveAsync($"events:{eventEntity.BusinessId}"); // ✅ Invalidate Cache
        }

        public async Task UpdateAsync(Event eventEntity)
        {
            _context.Events.Update(eventEntity);
            await _context.SaveChangesAsync();
            await _cacheService.RemoveAsync($"event:{eventEntity.Id}");
            await _cacheService.RemoveAsync($"events:{eventEntity.BusinessId}"); // ✅ Invalidate Cache
        }

        public async Task DeleteAsync(Guid eventId)
        {
            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity is not null)
            {
                _context.Events.Remove(eventEntity);
                await _context.SaveChangesAsync();
                await _cacheService.RemoveAsync($"event:{eventId}");
                await _cacheService.RemoveAsync($"events:{eventEntity.BusinessId}"); // ✅ Invalidate Cache
            }
        }

        public async Task<IEnumerable<Event>> GetScheduledEventsAsync(DateTime scheduledBefore)
        {
            var cacheKey = $"scheduled-events:{scheduledBefore:yyyy-MM-dd-HH}";

            // ✅ Try retrieving cached data
            var cachedEvents = await _cacheService.GetAsync<List<Event>>(cacheKey);
            if (cachedEvents is not null)
                return cachedEvents;

            // ✅ Optimized query (ensures index usage for performance)
            var events = await _context.Events
                                       .Where(e => e.ScheduledAt <= scheduledBefore)
                                       .OrderBy(e => e.ScheduledAt)
                                       .ToListAsync();

            if (events.Any())
            {
                await _cacheService.SetAsync(cacheKey, events, TimeSpan.FromMinutes(10)); // ✅ Cache for 10 min
            }

            return events;
        }

        public async Task<List<Event>> GetEventsByBusinessIdAsync(Guid businessId, CancellationToken stoppingToken)
        {
            var cacheKey = $"events:{businessId}";
            var cachedEvents = await _cacheService.GetAsync<List<Event>>(cacheKey);
            if (cachedEvents is not null)
                return cachedEvents;

            var events = await _context.Events
                                       .Where(e => e.BusinessId == businessId)
                                       .OrderBy(e => e.ScheduledAt)
                                       .ToListAsync(stoppingToken);
            if (events.Any())
            {
                await _cacheService.SetAsync(cacheKey, events, TimeSpan.FromMinutes(10)); // ✅ Cache for 10 min
            }

            return events;
        }

        public async Task<IEnumerable<Event>> GetRecurringEventsAsync(DateTime scheduledBefore)
        {
            return await _context.Events
                .Where(e => e.ScheduledAt <= scheduledBefore && e.Recurrence != null)
                .ToListAsync();
        }
    }
}
