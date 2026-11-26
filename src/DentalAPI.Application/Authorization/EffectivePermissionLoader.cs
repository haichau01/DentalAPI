using DentalAPI.Application.StaffAccounts;
using DentalAPI.Domain.Authorization;

namespace DentalAPI.Application.Authorization;

/// <summary>Reads a staff member's role permissions and overrides and combines them, without caching.</summary>
public sealed class EffectivePermissionLoader(
    IStaffAccountRepository staffAccountRepository,
    IPermissionRepository permissionRepository)
{
    public async Task<IReadOnlySet<Permission>> LoadAsync(Guid staffAccountId, CancellationToken cancellationToken)
    {
        var account = await staffAccountRepository.GetByIdAsync(staffAccountId, cancellationToken);
        if (account is null)
        {
            return new HashSet<Permission>();
        }

        var rolePermissions = await permissionRepository.ListForRoleAsync(account.Role, cancellationToken);
        var overrides = await permissionRepository.ListOverridesAsync(staffAccountId, cancellationToken);

        return EffectivePermissions.Calculate(account.Role, rolePermissions.Select(item => item.Permission), overrides);
    }
}
