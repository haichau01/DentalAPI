using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Authorization;
using FluentAssertions;

namespace DentalAPI.UnitTests.Authorization;

public sealed class EffectivePermissionsTests
{
    private static readonly Guid _staffId = Guid.NewGuid();

    [Fact]
    public void Calculate_RoleOnly_ReturnsRolePermissions()
    {
        var result = EffectivePermissions.Calculate(StaffRole.Dentist, [Permission.PatientsRead], []);

        result.Should().BeEquivalentTo([Permission.PatientsRead]);
    }

    [Fact]
    public void Calculate_GrantAndRevoke_AddsGrantedAndRemovesRevoked()
    {
        var overrides = new[]
        {
            Override(Permission.PatientsDelete, PermissionEffect.Grant),
            Override(Permission.AppointmentsRead, PermissionEffect.Revoke),
        };

        var result = EffectivePermissions.Calculate(
            StaffRole.Receptionist, [Permission.PatientsRead, Permission.AppointmentsRead], overrides);

        result.Should().BeEquivalentTo([Permission.PatientsRead, Permission.PatientsDelete]);
    }

    [Fact]
    public void Calculate_RevokeOfRolePermission_WinsOverRole()
    {
        var result = EffectivePermissions.Calculate(
            StaffRole.Dentist, [Permission.PatientsRead], [Override(Permission.PatientsRead, PermissionEffect.Revoke)]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_Admin_AlwaysHasEveryPermissionEvenWhenRevoked()
    {
        var revokeAll = PermissionCodes.All.Select(permission => Override(permission, PermissionEffect.Revoke));

        var result = EffectivePermissions.Calculate(StaffRole.Admin, [], revokeAll);

        result.Should().BeEquivalentTo(PermissionCodes.All);
    }

    private static StaffPermissionOverride Override(Permission permission, PermissionEffect effect) =>
        new(_staffId, permission, effect);
}
