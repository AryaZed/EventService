using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Caching;
using EventService.Domain.Entities.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Services.Events
{
    public class EventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly ICacheService _cacheService;

        public EventService(IEventRepository eventRepository, ICacheService cacheService)
        {
            _eventRepository = eventRepository;
            _cacheService = cacheService;
        }

        public async Task<List<Event>> GetEventsWithPrefetching(Guid businessId, CancellationToken stoppingToken)
        {
            // ✅ Prefetch top 5 upcoming events for this business
            var upcomingEventsKey = $"events:upcoming:{businessId}";
            var cachedUpcomingEvents = await _cacheService.GetAsync<List<Event>>(upcomingEventsKey);

            if (cachedUpcomingEvents is null)
            {
                var allEvents = await _eventRepository.GetEventsByBusinessIdAsync(businessId,stoppingToken);
                var upcomingEvents = allEvents.Where(e => e.ScheduledAt >= DateTime.UtcNow)
                                              .OrderBy(e => e.ScheduledAt)
                                              .Take(5)
                                              .ToList();

                await _cacheService.SetAsync(upcomingEventsKey, upcomingEvents, TimeSpan.FromMinutes(5)); // ✅ Prefetch cache
                return upcomingEvents;
            }

            return cachedUpcomingEvents;
        }
    }
}
