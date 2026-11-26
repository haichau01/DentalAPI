using System.Net;
using System.Net.Http.Json;
using DentalAPI.Application.Authorization;
using DentalAPI.Application.Patients;
using DentalAPI.Domain.Accounts;
using DentalAPI.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace DentalAPI.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class PermissionsEndpointTests(DentalApiFactory factory) : IAsyncLifetime
{
    private const string PatientsUrl = "/api/v1/patients";
    private const string DentistRolePermissionsUrl = "/api/v1/staff-roles/Dentist/permissions";

    private HttpClient _admin = null!;

    public async Task InitializeAsync() => _admin = await factory.CreateAdminClientAsync();

    public Task DisposeAsync()
    {
        _admin.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ListPermissions_Admin_ReturnsEveryCodeWithDescription()
    {
        var permissions = await _admin.GetFromJsonAsync<List<PermissionDto>>("/api/v1/permissions");

        permissions.Should().Contain(permission => permission.Code == "patients.delete" && permission.Description.Length > 0);
    }

    [Fact]
    public async Task GrantOverride_ReceptionistGetsDeletePermission_DeleteWorksWithoutSigningInAgain()
    {
        var receptionist = await factory.CreateStaffClientAsync(StaffRole.Receptionist);
        var patientId = await CreatePatientAsync();
        var before = await receptionist.Client.DeleteAsync($"{PatientsUrl}/{patientId}");

        var grant = await _admin.PutAsJsonAsync(
            OverridesUrl(receptionist.AccountId), new ReplacePermissionOverridesRequest(["patients.delete"], []));
        var after = await receptionist.Client.DeleteAsync($"{PatientsUrl}/{patientId}");

        before.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        grant.StatusCode.Should().Be(HttpStatusCode.OK);
        after.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RevokeOverride_DentistLosesReadPermission_Returns403()
    {
        var dentist = await factory.CreateStaffClientAsync(StaffRole.Dentist);
        var before = await dentist.Client.GetAsync(PatientsUrl);

        await _admin.PutAsJsonAsync(OverridesUrl(dentist.AccountId), new ReplacePermissionOverridesRequest([], ["patients.read"]));
        var after = await dentist.Client.GetAsync(PatientsUrl);
        var permissions = await _admin.GetFromJsonAsync<StaffAccountPermissionsDto>(
            $"{TestAccounts.StaffAccountsUrl}/{dentist.AccountId}/permissions", TestAccounts.JsonOptions);

        before.StatusCode.Should().Be(HttpStatusCode.OK);
        after.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        permissions!.EffectivePermissions.Should().NotContain("patients.read");
        permissions.Revokes.Should().Equal("patients.read");
    }

    [Fact]
    public async Task Override_GrantAndRevokeSameCode_Returns400()
    {
        var dentist = await factory.CreateStaffAccountAsync(StaffRole.Dentist);

        var response = await _admin.PutAsJsonAsync(
            OverridesUrl(dentist.Id), new ReplacePermissionOverridesRequest(["patients.write"], ["patients.write"]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReplaceRolePermissions_DentistRoleWithoutRead_AppliesToEveryDentist()
    {
        var dentist = await factory.CreateStaffClientAsync(StaffRole.Dentist);
        var original = await _admin.GetFromJsonAsync<StaffRolePermissionsDto>(DentistRolePermissionsUrl, TestAccounts.JsonOptions);
        var withoutRead = original!.Permissions.Where(code => code != "patients.read").ToList();

        try
        {
            var replace = await _admin.PutAsJsonAsync(DentistRolePermissionsUrl, new ReplaceRolePermissionsRequest(withoutRead));
            var response = await dentist.Client.GetAsync(PatientsUrl);

            replace.StatusCode.Should().Be(HttpStatusCode.OK);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            // Role permissions are shared by every test in the collection, so put them back.
            await _admin.PutAsJsonAsync(DentistRolePermissionsUrl, new ReplaceRolePermissionsRequest(original.Permissions));
        }
    }

    [Fact]
    public async Task ReplaceRolePermissions_ByReceptionist_Returns403()
    {
        var receptionist = await factory.CreateStaffClientAsync(StaffRole.Receptionist);

        var response = await receptionist.Client.PutAsJsonAsync(
            DentistRolePermissionsUrl, new ReplaceRolePermissionsRequest(["patients.read"]));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReplaceRolePermissions_AdminRole_Returns400()
    {
        var response = await _admin.PutAsJsonAsync(
            "/api/v1/staff-roles/Admin/permissions", new ReplaceRolePermissionsRequest(["patients.read"]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static string OverridesUrl(Guid staffAccountId) =>
        $"{TestAccounts.StaffAccountsUrl}/{staffAccountId}/permission-overrides";

    private async Task<Guid> CreatePatientAsync()
    {
        var request = new SavePatientRequest("Benh Nhan Xoa", new DateOnly(1990, 1, 1), TestAccounts.NewPhoneNumber(), null, null);
        var response = await _admin.PostAsJsonAsync(PatientsUrl, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PatientDto>())!.Id;
    }
}
