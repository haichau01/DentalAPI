using DentalAPI.Application.Common.Exceptions;
using DentalAPI.Application.Common.Persistence;
using DentalAPI.Application.StaffAccounts;
using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Authorization;
using FluentValidation;
using FluentValidation.Results;

namespace DentalAPI.Application.Authorization;

/// <summary>Admin use cases for configuring what each role and each staff member may do.</summary>
public interface IPermissionManagementService
{
    IReadOnlyList<PermissionDto> ListPermissions();

    Task<StaffRolePermissionsDto> GetRolePermissionsAsync(StaffRole role, CancellationToken cancellationToken);

    /// <exception cref="ValidationException">Unknown permission codes, or the Admin role (which always has every permission).</exception>
    Task<StaffRolePermissionsDto> ReplaceRolePermissionsAsync(
        StaffRole role, ReplaceRolePermissionsRequest request, CancellationToken cancellationToken);

    /// <exception cref="NotFoundException">The staff account does not exist.</exception>
    Task<StaffAccountPermissionsDto> GetAccountPermissionsAsync(Guid staffAccountId, CancellationToken cancellationToken);

    /// <exception cref="ValidationException">Unknown codes, or a code both granted and revoked.</exception>
    /// <exception cref="NotFoundException">The staff account does not exist.</exception>
    /// <exception cref="AdminAccountProtectedException">The account is an admin.</exception>
    Task<StaffAccountPermissionsDto> ReplaceOverridesAsync(
        Guid staffAccountId, ReplacePermissionOverridesRequest request, CancellationToken cancellationToken);
}

public sealed class PermissionManagementService(
    IPermissionRepository permissionRepository,
    IStaffAccountRepository staffAccountRepository,
    IEffectivePermissionProvider permissionProvider,
    IUnitOfWork unitOfWork,
    IValidator<ReplaceRolePermissionsRequest> rolePermissionsValidator,
    IValidator<ReplacePermissionOverridesRequest> overridesValidator) : IPermissionManagementService
{
    public IReadOnlyList<PermissionDto> ListPermissions() => PermissionCatalog.All;

    public async Task<StaffRolePermissionsDto> GetRolePermissionsAsync(StaffRole role, CancellationToken cancellationToken)
    {
        var rolePermissions = await permissionRepository.ListForRoleAsync(role, cancellationToken);
        var permissions = role == StaffRole.Admin ? PermissionCodes.All : rolePermissions.Select(item => item.Permission);
        return new StaffRolePermissionsDto(role, ToSortedCodes(permissions));
    }

    public async Task<StaffRolePermissionsDto> ReplaceRolePermissionsAsync(
        StaffRole role, ReplaceRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        await rolePermissionsValidator.ValidateAndThrowAsync(request, cancellationToken);
        if (role == StaffRole.Admin)
        {
            throw new ValidationException([new ValidationFailure("role", "Quyền của Admin cố định, không thể thay đổi.")]);
        }

        var wanted = ParseCodes(request.Permissions);
        var current = await permissionRepository.ListForRoleAsync(role, cancellationToken);

        foreach (var removed in current.Where(item => !wanted.Contains(item.Permission)))
        {
            permissionRepository.Remove(removed);
        }

        foreach (var added in wanted.Except(current.Select(item => item.Permission)))
        {
            permissionRepository.Add(new StaffRolePermission(role, added));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        permissionProvider.InvalidateAll();

        return new StaffRolePermissionsDto(role, ToSortedCodes(wanted));
    }

    public async Task<StaffAccountPermissionsDto> GetAccountPermissionsAsync(Guid staffAccountId, CancellationToken cancellationToken)
    {
        var account = await GetExistingAccountAsync(staffAccountId, cancellationToken);
        var overrides = await permissionRepository.ListOverridesAsync(staffAccountId, cancellationToken);
        var effective = await permissionProvider.GetAsync(staffAccountId, cancellationToken);

        return ToAccountPermissionsDto(account, effective, overrides);
    }

    public async Task<StaffAccountPermissionsDto> ReplaceOverridesAsync(
        Guid staffAccountId, ReplacePermissionOverridesRequest request, CancellationToken cancellationToken)
    {
        await overridesValidator.ValidateAndThrowAsync(request, cancellationToken);

        var account = await GetExistingAccountAsync(staffAccountId, cancellationToken);
        if (account.IsAdmin)
        {
            throw new AdminAccountProtectedException();
        }

        var wanted = BuildWantedEffects(request);
        var current = await permissionRepository.ListOverridesAsync(staffAccountId, cancellationToken);
        ApplyOverrideChanges(staffAccountId, current, wanted);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        permissionProvider.InvalidateAccount(staffAccountId);

        var effective = await permissionProvider.GetAsync(staffAccountId, cancellationToken);
        var overrides = wanted.Select(pair => new StaffPermissionOverride(staffAccountId, pair.Key, pair.Value));
        return ToAccountPermissionsDto(account, effective, overrides);
    }

    /// <summary>
    /// Updates existing rows in place instead of deleting and re-inserting them: deletes are soft, so a
    /// re-inserted row for the same permission would clash with the unique index within one save.
    /// </summary>
    private void ApplyOverrideChanges(
        Guid staffAccountId, IReadOnlyList<StaffPermissionOverride> current, Dictionary<Permission, PermissionEffect> wanted)
    {
        foreach (var existing in current)
        {
            if (wanted.TryGetValue(existing.Permission, out var effect))
            {
                existing.ChangeEffect(effect);
            }
            else
            {
                permissionRepository.Remove(existing);
            }
        }

        var existingPermissions = current.Select(item => item.Permission).ToHashSet();
        foreach (var (permission, effect) in wanted.Where(pair => !existingPermissions.Contains(pair.Key)))
        {
            permissionRepository.Add(new StaffPermissionOverride(staffAccountId, permission, effect));
        }
    }

    private async Task<StaffAccount> GetExistingAccountAsync(Guid staffAccountId, CancellationToken cancellationToken) =>
        await staffAccountRepository.GetByIdAsync(staffAccountId, cancellationToken)
        ?? throw new NotFoundException(nameof(StaffAccount), staffAccountId);

    private static Dictionary<Permission, PermissionEffect> BuildWantedEffects(ReplacePermissionOverridesRequest request) =>
        ParseCodes(request.Grants).Select(permission => (Permission: permission, Effect: PermissionEffect.Grant))
            .Concat(ParseCodes(request.Revokes).Select(permission => (Permission: permission, Effect: PermissionEffect.Revoke)))
            .ToDictionary(pair => pair.Permission, pair => pair.Effect);

    private static StaffAccountPermissionsDto ToAccountPermissionsDto(
        StaffAccount account, IEnumerable<Permission> effective, IEnumerable<StaffPermissionOverride> overrides)
    {
        var overrideList = overrides.ToList();
        return new StaffAccountPermissionsDto(
            account.Id,
            account.Role,
            ToSortedCodes(effective),
            ToSortedCodes(overrideList.Where(item => item.Effect == PermissionEffect.Grant).Select(item => item.Permission)),
            ToSortedCodes(overrideList.Where(item => item.Effect == PermissionEffect.Revoke).Select(item => item.Permission)));
    }

    private static HashSet<Permission> ParseCodes(IEnumerable<string> codes) =>
        codes.Select(PermissionCodes.FromCode).ToHashSet();

    private static List<string> ToSortedCodes(IEnumerable<Permission> permissions) =>
        permissions.Select(PermissionCodes.ToCode).Order(StringComparer.Ordinal).ToList();
}
