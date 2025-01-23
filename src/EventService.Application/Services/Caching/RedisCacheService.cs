using EventService.Application.Interfaces.Services.Caching;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EventService.Application.Services.Caching
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _database;

        public RedisCacheService(IDistributedCache cache, ConnectionMultiplexer redis)
        {
            _cache = cache;
            _redis = redis;
            _database = _redis.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var cachedData = await _cache.GetStringAsync(key);
            return string.IsNullOrEmpty(cachedData) ? default : JsonSerializer.Deserialize<T>(cachedData);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration,
                SlidingExpiration = TimeSpan.FromMinutes(expiration.TotalMinutes / 2) // ✅ Sliding Expiration
            };

            var serializedValue = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, serializedValue, options);
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }

        public async Task<List<string>> GetKeysAsync(string pattern)
        {
            var keys = new List<string>();
            var server = _redis.GetServer(_redis.GetEndPoints().First());

            foreach (var key in server.Keys(pattern: pattern))
            {
                keys.Add(key.ToString());
            }

            return await Task.FromResult(keys);
        }
    }
}
