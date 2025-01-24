using EventService.Domain.Entities.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Interfaces.Repositories;

public interface IEventRepository
{
    Task<IEnumerable<Event>> GetAllAsync();
    Task<Event?> GetByIdAsync(Guid id);
    Task AddAsync(Event eventEntity);
    Task UpdateAsync(Event eventEntity);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Event>> GetScheduledEventsAsync(DateTime scheduledBefore);
    Task<List<Event>> GetEventsByBusinessIdAsync(Guid businessId, CancellationToken stoppingToken);
    Task<IEnumerable<Event>> GetRecurringEventsAsync(DateTime now);
}
