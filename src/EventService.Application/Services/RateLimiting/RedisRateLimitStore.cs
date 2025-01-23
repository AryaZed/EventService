using EventService.Application.Interfaces.Services.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Services.RateLimiting
{
    public class RedisRateLimitStore : IRateLimitStore
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisRateLimitStore> _logger;

        public RedisRateLimitStore(IDistributedCache cache, ILogger<RedisRateLimitStore> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool> IsAllowedAsync(Guid tenantId, int maxPerMinute, int maxPerHour)
        {
            var minuteKey = $"rate-limit:{tenantId}:minute";
            var hourKey = $"rate-limit:{tenantId}:hour";

            int minuteCount = await GetRequestCountAsync(minuteKey);
            int hourCount = await GetRequestCountAsync(hourKey);

            if (minuteCount >= maxPerMinute || hourCount >= maxPerHour)
            {
                _logger.LogWarning("Rate limit exceeded for Tenant {TenantId}", tenantId);
                return false;
            }

            await IncrementRequestCountAsync(minuteKey, TimeSpan.FromMinutes(1));
            await IncrementRequestCountAsync(hourKey, TimeSpan.FromHours(1));

            return true;
        }

        private async Task<int> GetRequestCountAsync(string key)
        {
            var value = await _cache.GetStringAsync(key);
            return int.TryParse(value, out var count) ? count : 0;
        }

        private async Task IncrementRequestCountAsync(string key, TimeSpan expiry)
        {
            var count = await GetRequestCountAsync(key) + 1;
            await _cache.SetStringAsync(key, count.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            });
        }
    }
}
