using EventService.Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Interfaces.Repositories
{
    public interface IUserGroupRepository
    {
        Task<IEnumerable<UserGroup>> GetAllByBusinessIdAsync(Guid businessId);
        Task<UserGroup?> GetByIdAsync(Guid id);
        Task AddAsync(UserGroup group);
        Task UpdateAsync(UserGroup group);
        Task DeleteAsync(Guid id);
    }
}
