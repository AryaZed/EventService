using EventService.Application.Interfaces.Repositories;
using EventService.Domain.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EventService.Infrastructure.Persistence.Repositories;

public class EventRepository : IEventRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;

    public EventRepository(ApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<IEnumerable<Event>> GetAllAsync() =>
        await _context.Events.Include(e => e.Business).ToListAsync();

    public async Task<Event?> GetByIdAsync(Guid id) =>
        await _context.Events.Include(e => e.Business).FirstOrDefaultAsync(e => e.Id == id);

    public async Task AddAsync(Event eventEntity)
    {
        await _context.Events.AddAsync(eventEntity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Event eventEntity)
    {
        _context.Events.Update(eventEntity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var eventEntity = await _context.Events.FindAsync(id);
        if (eventEntity != null)
        {
            _context.Events.Remove(eventEntity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Event>> GetScheduledEventsAsync(DateTime scheduledBefore)
    {
        var cacheKey = $"scheduled-events-{scheduledBefore:yyyy-MM-dd-HH}";
        var cachedEvents = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedEvents))
        {
            return JsonSerializer.Deserialize<List<Event>>(cachedEvents);
        }

        var events = await _context.Events.Where(e => e.ScheduledAt <= scheduledBefore).ToListAsync();
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(events), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        return events;
    }

}