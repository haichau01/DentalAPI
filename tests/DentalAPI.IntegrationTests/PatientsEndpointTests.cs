using System.Net;
using System.Net.Http.Json;
using DentalAPI.Application.Common.Addresses;
using DentalAPI.Application.Common.Pagination;
using DentalAPI.Application.Patients;
using DentalAPI.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace DentalAPI.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class PatientsEndpointTests(DentalApiFactory factory) : IAsyncLifetime
{
    private const string PatientsUrl = "/api/v1/patients";

    // Patient endpoints require a staff permission; the admin holds all of them.
    private HttpClient _client = null!;

    public async Task InitializeAsync() => _client = await factory.CreateAdminClientAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Create_WithAddress_ReturnsAddress()
    {
        var request = ValidRequest() with { Address = new AddressDto("12 Hang Bai", "Phuong Hoan Kiem", "Ha Noi") };

        var response = await _client.PostAsJsonAsync(PatientsUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<PatientDto>();
        created!.Address.Should().Be(request.Address);
    }

    [Fact]
    public async Task Create_AddressWithoutProvince_Returns400WithAddressError()
    {
        var request = ValidRequest() with { Address = new AddressDto("12 Hang Bai", "Phuong Hoan Kiem", string.Empty) };

        var response = await _client.PostAsJsonAsync(PatientsUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Keys.Should().Contain("Address.Province");
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocationOfNewPatient()
    {
        var response = await _client.PostAsJsonAsync(PatientsUrl, ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<PatientDto>();
        created!.FullName.Should().Be("Pham Van D");
        response.Headers.Location!.AbsolutePath.Should().Be($"{PatientsUrl}/{created.Id}");
    }

    [Fact]
    public async Task Create_InvalidRequest_Returns400WithFieldErrors()
    {
        var request = ValidRequest() with { FullName = string.Empty, PhoneNumber = "abc" };

        var response = await _client.PostAsJsonAsync(PatientsUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Keys.Should().Contain([nameof(SavePatientRequest.FullName), nameof(SavePatientRequest.PhoneNumber)]);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync($"{PatientsUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Update_ExistingPatient_ReturnsUpdatedPatient()
    {
        var created = await CreatePatientAsync();
        var request = ValidRequest() with { PhoneNumber = "0911111111" };

        var response = await _client.PutAsJsonAsync($"{PatientsUrl}/{created.Id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PatientDto>();
        updated!.PhoneNumber.Should().Be("0911111111");
    }

    [Fact]
    public async Task Delete_ExistingPatient_Returns204AndPatientIsNoLongerFound()
    {
        var created = await CreatePatientAsync();

        var deleteResponse = await _client.DeleteAsync($"{PatientsUrl}/{created.Id}");
        var getResponse = await _client.GetAsync($"{PatientsUrl}/{created.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_DefaultQuery_ReturnsSeededPatients()
    {
        var response = await _client.GetAsync(PatientsUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<PatientDto>>();
        page!.TotalCount.Should().BeGreaterThan(0);
        page.PageSize.Should().Be(PageQuery.DefaultPageSize);
    }

    [Fact]
    public async Task List_PageSizeAboveMaximum_Returns400()
    {
        var response = await _client.GetAsync($"{PatientsUrl}?pageSize={PageQuery.MaxPageSize + 1}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static SavePatientRequest ValidRequest() =>
        new("Pham Van D", new DateOnly(1995, 3, 20), "0900000004", "d@example.com", null);

    private async Task<PatientDto> CreatePatientAsync()
    {
        var response = await _client.PostAsJsonAsync(PatientsUrl, ValidRequest());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PatientDto>())!;
    }
}
