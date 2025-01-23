using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Caching;
using EventService.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ICacheService _cacheService;

    public UserRepository(ApplicationDbContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<List<User>> GetAllByBusinessIdAsync(Guid businessId)
    {
        var cacheKey = $"users:{businessId}";
        var cachedUsers = await _cacheService.GetAsync<List<User>>(cacheKey);
        if (cachedUsers is not null)
            return cachedUsers;

        var users = await _context.Users.Where(u => u.BusinessId == businessId).ToListAsync();
        if (users.Any())
        {
            await _cacheService.SetAsync(cacheKey, users, TimeSpan.FromMinutes(15)); // Cache for 15 min
        }

        return users;
    }

    public async Task<List<User>> GetUsersJoinedAfterAsync(Guid businessId, DateTime date) =>
        await _context.Users.Where(u => u.BusinessId == businessId && u.CreatedAt >= date).ToListAsync();

    public async Task<List<User>> GetUsersByIdsAsync(List<Guid> userIds) =>
        await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();

    public async Task<List<User>> GetUsersByGroupIdsAsync(List<Guid> groupIds) =>
        await _context.Users.Where(u => u.UserUserGroups.Any(g => groupIds.Contains(g.UserGroupId))).ToListAsync();

    public async Task<User?> GetByIdAsync(Guid id) =>
    await _context.Users.FindAsync(id);

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        await _cacheService.RemoveAsync($"users:{user.BusinessId}"); // ✅ Invalidate Cache
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}

