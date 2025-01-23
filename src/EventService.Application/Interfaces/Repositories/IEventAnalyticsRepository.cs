using EventService.Domain.Entities.Analytics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Interfaces.Repositories
{
    public interface IEventAnalyticsRepository
    {
        Task AddAsync(EventAnalytics analytics);
        Task<List<EventAnalytics>> GetByEventIdAsync(Guid eventId);
        Task<List<EventAnalytics>> GetAllAsync();
    }
}
