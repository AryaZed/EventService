using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Caching;
using EventService.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Infrastructure.Persistence.Repositories;

public class BusinessRepository : IBusinessRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ICacheService _cacheService;

    public BusinessRepository(ApplicationDbContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<IEnumerable<Business>> GetAllAsync() =>
        await _context.Businesses.ToListAsync();

    public async Task<Business?> GetByIdAsync(Guid id) =>
        await _context.Businesses.FindAsync(id);

    public async Task AddAsync(Business business)
    {
        await _context.Businesses.AddAsync(business);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Business business)
    {
        _context.Businesses.Update(business);
        await _context.SaveChangesAsync();
        await _cacheService.RemoveAsync($"business:{business.Id}"); // ✅ Invalidate Cache
    }

    public async Task DeleteAsync(Guid id)
    {
        var business = await _context.Businesses.FindAsync(id);
        if (business != null)
        {
            _context.Businesses.Remove(business);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Business>> GetExpiredSubscriptionsAsync()
    {
        return await _context.Businesses
            .Where(b => b.SubscriptionEndDate < DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task<Business?> GetBusinessByTenantAsync(Guid tenantId)
    {
        var cacheKey = $"business:{tenantId}";
        var cachedBusiness = await _cacheService.GetAsync<Business>(cacheKey);
        if (cachedBusiness is not null)
            return cachedBusiness;

        var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == tenantId);
        if (business is not null)
        {
            await _cacheService.SetAsync(cacheKey, business, TimeSpan.FromMinutes(30)); // Cache for 30 min
        }

        return business;
    }
}