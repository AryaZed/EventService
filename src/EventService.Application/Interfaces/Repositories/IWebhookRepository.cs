using EventService.Domain.Entities.Integrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Interfaces.Repositories
{
    public interface IWebhookRepository
    {
        Task<List<Webhook>> GetByBusinessIdAsync(Guid businessId);
        Task<Webhook?> GetByIdAsync(Guid id);
        Task AddAsync(Webhook webhook);
        Task UpdateAsync(Webhook webhook);
        Task DeleteAsync(Guid id);
    }
}
