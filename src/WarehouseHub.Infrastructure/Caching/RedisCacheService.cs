using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WarehouseHub.Application.Common.Interfaces;

namespace WarehouseHub.Infrastructure.Caching;

// Cache failures never break a request: reads fall back to the database,
// and a missed invalidation is bounded by the entry's TTL.
public class RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await cache.GetStringAsync(key, cancellationToken);
            if (json is null)
            {
                logger.LogInformation("Cache miss: {Key}", key);
                return default;
            }

            logger.LogInformation("Cache hit: {Key}", key);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache read failed for {Key}; falling back to the database.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan timeToLive, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = timeToLive };
            await cache.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache write failed for {Key}.", key);
        }
    }

    public async Task RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        foreach (var key in keys)
        {
            try
            {
                await cache.RemoveAsync(key, cancellationToken);
                logger.LogInformation("Cache invalidated: {Key}", key);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cache invalidation failed for {Key}; it will expire by TTL.", key);
            }
        }
    }
}
