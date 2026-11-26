using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Authorization;

namespace DentalAPI.Application.Authorization;

/// <summary>Loads and stores role permissions and per-account overrides. Returned entities are tracked.</summary>
public interface IPermissionRepository
{
    Task<IReadOnlyList<StaffRolePermission>> ListForRoleAsync(StaffRole role, CancellationToken cancellationToken);

    Task<IReadOnlyList<StaffPermissionOverride>> ListOverridesAsync(Guid staffAccountId, CancellationToken cancellationToken);

    void Add(StaffRolePermission rolePermission);

    /// <summary>Role permissions are configuration, not history, so they are deleted for real.</summary>
    void Remove(StaffRolePermission rolePermission);

    void Add(StaffPermissionOverride permissionOverride);

    void Remove(StaffPermissionOverride permissionOverride);
}

/// <summary>
/// Answers what a staff member may do right now. Implementations may cache, but must drop cached values
/// when told, so permission changes take effect without the staff member signing in again.
/// </summary>
public interface IEffectivePermissionProvider
{
    /// <summary>Returns an empty set for unknown or deleted accounts.</summary>
    Task<IReadOnlySet<Permission>> GetAsync(Guid staffAccountId, CancellationToken cancellationToken);

    void InvalidateAccount(Guid staffAccountId);

    /// <summary>Drops every cached value; used when a role's permissions change.</summary>
    void InvalidateAll();
}
