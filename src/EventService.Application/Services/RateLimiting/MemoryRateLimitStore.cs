using EventService.Application.Interfaces.Services.RateLimiting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Services.RateLimiting
{
    public class MemoryRateLimitStore : IRateLimitStore
    {
        private static readonly ConcurrentDictionary<string, (int Count, DateTime Expiry)> _cache = new();
        private readonly ILogger<MemoryRateLimitStore> _logger;

        public MemoryRateLimitStore(ILogger<MemoryRateLimitStore> logger)
        {
            _logger = logger;
        }

        public Task<bool> IsAllowedAsync(Guid tenantId, int maxPerMinute, int maxPerHour)
        {
            var now = DateTime.UtcNow;
            var minuteKey = $"rate-limit:{tenantId}:minute";
            var hourKey = $"rate-limit:{tenantId}:hour";

            if (ExceedsLimit(minuteKey, maxPerMinute, now) || ExceedsLimit(hourKey, maxPerHour, now))
            {
                _logger.LogWarning("Rate limit exceeded for Tenant {TenantId}", tenantId);
                return Task.FromResult(false);
            }

            IncrementRequestCount(minuteKey, now.AddMinutes(1));
            IncrementRequestCount(hourKey, now.AddHours(1));

            return Task.FromResult(true);
        }

        private bool ExceedsLimit(string key, int maxLimit, DateTime now)
        {
            if (_cache.TryGetValue(key, out var entry) && entry.Expiry > now)
            {
                return entry.Count >= maxLimit;
            }
            return false;
        }

        private void IncrementRequestCount(string key, DateTime expiry)
        {
            _cache.AddOrUpdate(key, (1, expiry), (_, old) => (old.Count + 1, expiry));
        }
    }
}
