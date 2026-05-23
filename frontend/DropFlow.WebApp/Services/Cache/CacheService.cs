using DropFlow.WebApp.Interfaces.Caches;
using Microsoft.Extensions.Caching.Memory;

namespace DropFlow.WebApp.Services.Cache;

public class CacheService(IMemoryCache cache, ILogger<CacheService> logger) : ICacheService
{
    private readonly HashSet<string> _keys = [];
    private readonly Lock _lock = new();

    public T? Get<T>(string key) where T : class
    {
        if (cache.TryGetValue(key, out T? value))
        {
            logger.LogDebug("✅ Cache HIT: {Key}", key);
            return value;
        }

        logger.LogDebug("❌ Cache MISS: {Key}", key);
        return null;
    }

    public void Set<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(10),
            SlidingExpiration = TimeSpan.FromMinutes(2),
            Priority = CacheItemPriority.Normal
        };

        // Callback pour nettoyer la clé quand l'entrée expire
        cacheOptions.RegisterPostEvictionCallback((k, v, reason, state) =>
        {
            lock (_lock)
            {
                _keys.Remove(k.ToString() ?? string.Empty);
            }
            logger.LogDebug("🗑️ Cache evicted: {Key} (Reason: {Reason})", k, reason);
        });

        cache.Set(key, value, cacheOptions);

        lock (_lock)
        {
            _keys.Add(key);
        }

        logger.LogDebug("💾 Cache SET: {Key} (expires in {Minutes} min)", 
            key, expiration?.TotalMinutes ?? 10);
    }

    public void Remove(string key)
    {
        cache.Remove(key);

        lock (_lock)
        {
            _keys.Remove(key);
        }

        logger.LogInformation("🗑️ Cache REMOVE: {Key}", key);
    }

    public bool Exists(string key)
    {
        return cache.TryGetValue(key, out _);
    }

    public void Clear()
    {
        lock (_lock)
        {
            foreach (var key in _keys.ToList())
            {
                cache.Remove(key);
            }
            _keys.Clear();
        }

        logger.LogInformation("🧹 Cache CLEARED - All entries removed");
    }
}