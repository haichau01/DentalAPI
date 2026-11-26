using DentalAPI.Application.Authorization;
using DentalAPI.Domain.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace DentalAPI.Infrastructure.Authorization;

/// <summary>
/// Process-wide cache of effective permissions, checked on every staff request. An in-memory cache is enough
/// while the API runs as one instance; with several instances this must move to a distributed cache,
/// otherwise a permission change would only take effect on the instance that handled it.
/// </summary>
public sealed class PermissionCache(IMemoryCache memoryCache)
{
    private static readonly TimeSpan _lifetime = TimeSpan.FromSeconds(60);

    // Bumping the generation makes every existing key unreachable; old entries then expire on their own.
    private int _generation;

    public bool TryGet(Guid staffAccountId, out IReadOnlySet<Permission>? permissions) =>
        memoryCache.TryGetValue(Key(staffAccountId), out permissions);

    public void Set(Guid staffAccountId, IReadOnlySet<Permission> permissions) =>
        memoryCache.Set(Key(staffAccountId), permissions, _lifetime);

    public void Remove(Guid staffAccountId) => memoryCache.Remove(Key(staffAccountId));

    public void Clear() => Interlocked.Increment(ref _generation);

    private string Key(Guid staffAccountId) => $"permissions:{Volatile.Read(ref _generation)}:{staffAccountId}";
}

public sealed class CachedEffectivePermissionProvider(PermissionCache cache, EffectivePermissionLoader loader)
    : IEffectivePermissionProvider
{
    public async Task<IReadOnlySet<Permission>> GetAsync(Guid staffAccountId, CancellationToken cancellationToken)
    {
        if (cache.TryGet(staffAccountId, out var cached) && cached is not null)
        {
            return cached;
        }

        var permissions = await loader.LoadAsync(staffAccountId, cancellationToken);
        cache.Set(staffAccountId, permissions);
        return permissions;
    }

    public void InvalidateAccount(Guid staffAccountId) => cache.Remove(staffAccountId);

    public void InvalidateAll() => cache.Clear();
}
