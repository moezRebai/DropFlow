using DropFlow.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace DropFlow.Infrastructure.Services;

public class TokenBlacklistService(IMemoryCache cache) : ITokenBlacklistService
{
    private const string Prefix = "revoked_jti_";

    public void Revoke(string jti, DateTime expiry)
    {
        var ttl = expiry - DateTime.UtcNow;
        if (ttl > TimeSpan.Zero)
            cache.Set(Prefix + jti, true, ttl);
    }

    public bool IsRevoked(string jti) => cache.TryGetValue(Prefix + jti, out _);
}
