using EventService.Application.Interfaces.Repositories;
using EventService.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Infrastructure.Persistence.Repositories;

public class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly ApplicationDbContext _context;

    public SubscriptionPlanRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionPlan?> GetByIdAsync(Guid id) =>
        await _context.SubscriptionPlans.FindAsync(id);

    public async Task<List<SubscriptionPlan>> GetAllAsync() =>
        await _context.SubscriptionPlans.ToListAsync();

    public async Task AddAsync(SubscriptionPlan plan)
    {
        await _context.SubscriptionPlans.AddAsync(plan);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SubscriptionPlan plan)
    {
        _context.SubscriptionPlans.Update(plan);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(id);
        if (plan != null)
        {
            _context.SubscriptionPlans.Remove(plan);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<SubscriptionPlan?> GetDefaultPlanAsync() =>
        await _context.SubscriptionPlans.FirstOrDefaultAsync(sp => sp.Name == "Basic");
}
