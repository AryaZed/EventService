using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Caching;
using EventService.Application.Services.Caching;
using EventService.Domain.Entities.Integrations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventService.Infrastructure.Persistence.Repositories
{
    public class WebhookRepository : IWebhookRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;

        public WebhookRepository(ApplicationDbContext context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        public async Task<List<Webhook>> GetByBusinessIdAsync(Guid businessId)
        {
            var cacheKey = $"webhooks:{businessId}";
            var cachedWebhooks = await _cacheService.GetAsync<List<Webhook>>(cacheKey);

            if (cachedWebhooks is not null)
                return cachedWebhooks;

            var webhooks = await _context.Webhooks.Where(w => w.BusinessId == businessId).ToListAsync();

            if (webhooks.Any())
                await _cacheService.SetAsync(cacheKey, webhooks, TimeSpan.FromMinutes(30));

            return webhooks;
        }

        public async Task<Webhook?> GetByIdAsync(Guid id) =>
            await _context.Webhooks.FindAsync(id);

        public async Task AddAsync(Webhook webhook)
        {
            await _context.Webhooks.AddAsync(webhook);
            await _context.SaveChangesAsync();
            await _cacheService.RemoveAsync($"webhooks:{webhook.BusinessId}");
        }

        public async Task UpdateAsync(Webhook webhook)
        {
            _context.Webhooks.Update(webhook);
            await _context.SaveChangesAsync();
            await _cacheService.RemoveAsync($"webhooks:{webhook.BusinessId}");
        }

        public async Task DeleteAsync(Guid id)
        {
            var webhook = await _context.Webhooks.FindAsync(id);
            if (webhook is not null)
            {
                _context.Webhooks.Remove(webhook);
                await _context.SaveChangesAsync();
                await _cacheService.RemoveAsync($"webhooks:{webhook.BusinessId}");
            }
        }
    }
}
