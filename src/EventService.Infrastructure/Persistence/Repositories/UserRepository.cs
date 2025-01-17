using EventService.Application.Interfaces.Repositories;
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

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<User>> GetAllByBusinessIdAsync(Guid businessId) =>
        await _context.Users.Where(u => u.BusinessId == businessId).ToListAsync();

    public async Task<List<User>> GetUsersJoinedAfterAsync(Guid businessId, DateTime date) =>
        await _context.Users.Where(u => u.BusinessId == businessId && u.CreatedAt >= date).ToListAsync();

    public async Task<List<User>> GetUsersByIdsAsync(List<Guid> userIds) =>
        await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();

    public async Task<List<User>> GetUsersByGroupIdsAsync(List<Guid> groupIds) =>
        await _context.Users.Where(u => u.Groups.Any(g => groupIds.Contains(g.Id))).ToListAsync();

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

