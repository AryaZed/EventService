using EventService.Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<List<User>> GetAllByBusinessIdAsync(Guid businessId);
    Task<List<User>> GetUsersJoinedAfterAsync(Guid businessId, DateTime date);
    Task<List<User>> GetUsersByIdsAsync(List<Guid> userIds);
    Task<List<User>> GetUsersByGroupIdsAsync(List<Guid> groupIds);
    Task<User?> GetByIdAsync(Guid id);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(Guid id);
}
