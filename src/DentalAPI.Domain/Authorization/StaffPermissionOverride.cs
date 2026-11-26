using DentalAPI.Domain.Common;

namespace DentalAPI.Domain.Authorization;

/// <summary>An extra permission granted to, or a role permission revoked from, one staff member.</summary>
public sealed class StaffPermissionOverride : Entity
{
    // Required by EF Core to materialize entities from the database.
    private StaffPermissionOverride()
    {
    }

    public StaffPermissionOverride(Guid staffAccountId, Permission permission, PermissionEffect effect)
    {
        StaffAccountId = staffAccountId;
        Permission = permission;
        Effect = effect;
    }

    public Guid StaffAccountId { get; private set; }

    public Permission Permission { get; private set; }

    public PermissionEffect Effect { get; private set; }

    public void ChangeEffect(PermissionEffect effect)
    {
        Effect = effect;
    }
}
