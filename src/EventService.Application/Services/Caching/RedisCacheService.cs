using EventService.Application.Interfaces.Services.Caching;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
            if (string.IsNullOrEmpty(cachedData)) return default;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, // ✅ Ensure case-insensitive matching
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };

            return JsonSerializer.Deserialize<T>(cachedData, options);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
        {
            var options = new JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve, // ✅ Fix circular dependencies
                PropertyNameCaseInsensitive = true
            };

            var serializedValue = JsonSerializer.Serialize(value, options);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration,
                SlidingExpiration = TimeSpan.FromMinutes(expiration.TotalMinutes / 2) // ✅ Sliding Expiration
            };

            await _cache.SetStringAsync(key, serializedValue, cacheOptions);
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
