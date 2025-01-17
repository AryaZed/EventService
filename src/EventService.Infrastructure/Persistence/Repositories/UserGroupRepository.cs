using EventService.Application.Interfaces.Repositories;
using EventService.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Infrastructure.Persistence.Repositories;

public class UserGroupRepository : IUserGroupRepository
{
    private readonly ApplicationDbContext _context;

    public UserGroupRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserGroup>> GetAllByBusinessIdAsync(Guid businessId) =>
        await _context.UserGroups.Where(g => g.BusinessId == businessId).ToListAsync();

    public async Task<UserGroup?> GetByIdAsync(Guid id) =>
        await _context.UserGroups.FindAsync(id);

    public async Task AddAsync(UserGroup group)
    {
        await _context.UserGroups.AddAsync(group);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserGroup group)
    {
        _context.UserGroups.Update(group);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var group = await _context.UserGroups.FindAsync(id);
        if (group != null)
        {
            _context.UserGroups.Remove(group);
            await _context.SaveChangesAsync();
        }
    }
}
