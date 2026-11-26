using DentalAPI.Domain.Accounts;

namespace DentalAPI.Domain.Authorization;

/// <summary>Works out what a staff member may actually do.</summary>
public static class EffectivePermissions
{
    /// <summary>
    /// Role permissions plus individual grants, minus individual revokes. A revoke wins over both,
    /// so an admin can always take a permission away from one person. Admins always hold every
    /// permission, so nobody can lock the clinic out of its own system.
    /// </summary>
    public static IReadOnlySet<Permission> Calculate(
        StaffRole role,
        IEnumerable<Permission> rolePermissions,
        IEnumerable<StaffPermissionOverride> overrides)
    {
        if (role == StaffRole.Admin)
        {
            return PermissionCodes.All.ToHashSet();
        }

        var overrideList = overrides.ToList();
        var granted = rolePermissions
            .Concat(overrideList.Where(item => item.Effect == PermissionEffect.Grant).Select(item => item.Permission))
            .ToHashSet();
        granted.ExceptWith(overrideList.Where(item => item.Effect == PermissionEffect.Revoke).Select(item => item.Permission));

        return granted;
    }
}
