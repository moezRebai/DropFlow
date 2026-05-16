using DropFlow.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace DropFlow.Infrastructure.Services;

public class AppCacheService(IMemoryCache cache) : IAppCacheService
{
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl)
    {
        if (cache.TryGetValue(key, out T? cached) && cached is not null)
            return cached;

        var value = await factory();

        // Never cache empty collections — avoids storing DB-error states in long-lived caches
        var isEmpty = value is System.Collections.ICollection { Count: 0 };
        if (!isEmpty && value is not null)
            cache.Set(key, value, ttl);

        return value;
    }

    public void Remove(string key) => cache.Remove(key);
}
