using EventService.Domain.Entities.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Interfaces.Repositories;

public interface ISubscriptionPlanRepository
{
    Task<SubscriptionPlan?> GetByIdAsync(Guid id);
    Task<List<SubscriptionPlan>> GetAllAsync();
    Task AddAsync(SubscriptionPlan plan);
    Task UpdateAsync(SubscriptionPlan plan);
    Task DeleteAsync(Guid id);
    Task<SubscriptionPlan?> GetDefaultPlanAsync(); // Default/Fallback Plan
}
