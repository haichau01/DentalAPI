using DentalAPI.Domain.Accounts;

namespace DentalAPI.Domain.Authorization;

/// <summary>A permission every staff member of a role has, unless revoked for that person.</summary>
public sealed class StaffRolePermission
{
    // Required by EF Core to materialize entities from the database.
    private StaffRolePermission()
    {
    }

    public StaffRolePermission(StaffRole role, Permission permission)
    {
        Role = role;
        Permission = permission;
    }

    public StaffRole Role { get; private set; }

    public Permission Permission { get; private set; }
}
