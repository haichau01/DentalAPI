using DentalAPI.Application.Authorization;
using DentalAPI.Application.Common.Exceptions;
using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Authorization;
using DentalAPI.UnitTests.Auth;
using FluentAssertions;
using FluentValidation;

namespace DentalAPI.UnitTests.Authorization;

public sealed class PermissionManagementServiceTests
{
    private readonly AuthTestHarness _harness = new();
    private readonly PermissionManagementService _service;

    public PermissionManagementServiceTests()
    {
        _service = _harness.CreatePermissionManagementService();
    }

    [Fact]
    public async Task ReplaceOverridesAsync_GrantAndRevoke_ReturnsEffectivePermissionsAndInvalidatesCache()
    {
        var receptionist = _harness.AddStaffAccount(StaffRole.Receptionist);
        var request = new ReplacePermissionOverridesRequest(["patients.delete"], ["invoices.write"]);

        var result = await _service.ReplaceOverridesAsync(receptionist.Id, request, CancellationToken.None);

        result.EffectivePermissions.Should().Contain("patients.delete").And.NotContain("invoices.write");
        result.Grants.Should().Equal("patients.delete");
        result.Revokes.Should().Equal("invoices.write");
        _harness.PermissionProvider.InvalidatedAccounts.Should().Equal(receptionist.Id);
        _harness.UnitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task ReplaceOverridesAsync_ExistingGrantBecomesRevoke_UpdatesRowInsteadOfAddingOne()
    {
        var dentist = _harness.AddStaffAccount(StaffRole.Dentist);
        await _service.ReplaceOverridesAsync(dentist.Id, new(["patients.write"], []), CancellationToken.None);

        await _service.ReplaceOverridesAsync(dentist.Id, new([], ["patients.write"]), CancellationToken.None);

        _harness.Permissions.Overrides.Should().ContainSingle()
            .Which.Effect.Should().Be(PermissionEffect.Revoke);
    }

    [Fact]
    public async Task ReplaceOverridesAsync_SameCodeGrantedAndRevoked_ThrowsValidationException()
    {
        var dentist = _harness.AddStaffAccount(StaffRole.Dentist);
        var request = new ReplacePermissionOverridesRequest(["patients.read"], ["PATIENTS.READ"]);

        var act = () => _service.ReplaceOverridesAsync(dentist.Id, request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _harness.UnitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ReplaceOverridesAsync_UnknownCode_ThrowsValidationException()
    {
        var dentist = _harness.AddStaffAccount(StaffRole.Dentist);

        var act = () => _service.ReplaceOverridesAsync(dentist.Id, new(["patients.fly"], []), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ReplaceOverridesAsync_AdminAccount_ThrowsAdminAccountProtectedException()
    {
        var admin = _harness.AddStaffAccount(StaffRole.Admin);

        var act = () => _service.ReplaceOverridesAsync(admin.Id, new([], ["patients.read"]), CancellationToken.None);

        await act.Should().ThrowAsync<AdminAccountProtectedException>();
    }

    [Fact]
    public async Task ReplaceRolePermissionsAsync_Dentist_ReplacesSetAndInvalidatesAllCachedPermissions()
    {
        var request = new ReplaceRolePermissionsRequest(["patients.read", "invoices.read"]);

        var result = await _service.ReplaceRolePermissionsAsync(StaffRole.Dentist, request, CancellationToken.None);

        result.Permissions.Should().Equal("invoices.read", "patients.read");
        _harness.Permissions.RolePermissions.Where(item => item.Role == StaffRole.Dentist)
            .Select(item => item.Permission)
            .Should().BeEquivalentTo([Permission.PatientsRead, Permission.InvoicesRead]);
        _harness.PermissionProvider.InvalidateAllCount.Should().Be(1);
    }

    [Fact]
    public async Task ReplaceRolePermissionsAsync_AdminRole_ThrowsValidationException()
    {
        var act = () => _service.ReplaceRolePermissionsAsync(StaffRole.Admin, new([]), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
