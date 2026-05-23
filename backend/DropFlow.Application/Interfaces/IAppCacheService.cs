namespace DropFlow.Application.Interfaces;

public interface IAppCacheService
{
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl);
    void Remove(string key);
}
