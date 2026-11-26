using System.Net;
using System.Net.Http.Json;
using DentalAPI.Application.Common.Addresses;
using DentalAPI.Application.Patients;
using DentalAPI.Domain.Accounts;
using DentalAPI.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace DentalAPI.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class AuthorizationTests(DentalApiFactory factory)
{
    private const string PatientsUrl = "/api/v1/patients";
    private const string ProfileUrl = "/api/v1/me/profile";

    [Fact]
    public async Task GetPatients_WithoutToken_Returns401ProblemDetails()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(PatientsUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task GetPatients_TamperedToken_Returns401()
    {
        var patient = await factory.CreatePatientClientAsync();
        patient.Client.DefaultRequestHeaders.Authorization = new("Bearer", patient.Tokens.AccessToken[..^4] + "AAAA");

        var response = await patient.Client.GetAsync(ProfileUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPatients_PatientToken_Returns403()
    {
        var patient = await factory.CreatePatientClientAsync();

        var response = await patient.Client.GetAsync(PatientsUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPatients_ReceptionistToken_Returns200()
    {
        var receptionist = await factory.CreateStaffClientAsync(StaffRole.Receptionist);

        var response = await receptionist.Client.GetAsync(PatientsUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeletePatient_ReceptionistWithoutDeletePermission_Returns403()
    {
        var receptionist = await factory.CreateStaffClientAsync(StaffRole.Receptionist);

        var response = await receptionist.Client.DeleteAsync($"{PatientsUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreatePatient_DentistWithoutWritePermission_Returns403()
    {
        var dentist = await factory.CreateStaffClientAsync(StaffRole.Dentist);
        var request = new SavePatientRequest("Pham Van F", new DateOnly(1990, 1, 1), TestAccounts.NewPhoneNumber(), null, null);

        var response = await dentist.Client.PostAsJsonAsync(PatientsUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetProfile_PatientToken_ReturnsOwnRecord()
    {
        var phone = TestAccounts.NewPhoneNumber();
        var patient = await factory.CreatePatientClientAsync(phone);

        var profile = await patient.Client.GetFromJsonAsync<PatientDto>(ProfileUrl);

        profile!.PhoneNumber.Should().Be(phone);
        profile.Address!.Province.Should().Be("Ha Noi");
    }

    [Fact]
    public async Task GetProfile_StaffToken_Returns403()
    {
        using var admin = await factory.CreateAdminClientAsync();

        var response = await admin.GetAsync(ProfileUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateAddress_PatientToken_ChangesOwnAddress()
    {
        var patient = await factory.CreatePatientClientAsync();
        var address = new AddressDto("5 Nguyen Hue", "Phuong Sai Gon", "TP Ho Chi Minh");

        var response = await patient.Client.PutAsJsonAsync("/api/v1/me/address", new UpdateAddressRequest(address));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PatientDto>();
        updated!.Address.Should().Be(address);
    }

    [Fact]
    public async Task OpenApi_WithoutToken_Returns200AndDeclaresBearerScheme()
    {
        using var client = factory.CreateClient();

        var document = await client.GetStringAsync("/openapi/v1.json");

        document.Should().Contain("\"bearer\"");
    }
}
