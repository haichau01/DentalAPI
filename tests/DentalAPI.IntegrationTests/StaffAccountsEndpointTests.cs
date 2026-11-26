using System.Net;
using System.Net.Http.Json;
using DentalAPI.Application.Common.Addresses;
using DentalAPI.Application.StaffAccounts;
using DentalAPI.Domain.Accounts;
using DentalAPI.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace DentalAPI.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class StaffAccountsEndpointTests(DentalApiFactory factory) : IAsyncLifetime
{
    private const string Url = TestAccounts.StaffAccountsUrl;

    private HttpClient _admin = null!;

    public async Task InitializeAsync() => _admin = await factory.CreateAdminClientAsync();

    public Task DisposeAsync()
    {
        _admin.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Create_DentistWithAddress_Returns201WithLocationAndAddress()
    {
        var request = ValidRequest(StaffRole.Dentist);

        var response = await _admin.PostAsJsonAsync(Url, request, TestAccounts.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<StaffAccountDto>(TestAccounts.JsonOptions);
        created!.Role.Should().Be(StaffRole.Dentist);
        created.Address.Should().Be(request.Address);
        created.MustChangePassword.Should().BeTrue();
        response.Headers.Location!.AbsolutePath.Should().Be($"{Url}/{created.Id}");
    }

    [Fact]
    public async Task Create_AdminRole_Returns400()
    {
        var response = await _admin.PostAsJsonAsync(Url, ValidRequest(StaffRole.Admin), TestAccounts.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Keys.Should().Contain(nameof(CreateStaffAccountRequest.Role));
    }

    [Fact]
    public async Task Create_DuplicateEmail_Returns409()
    {
        var request = ValidRequest(StaffRole.Receptionist);
        await _admin.PostAsJsonAsync(Url, request, TestAccounts.JsonOptions);

        var response = await _admin.PostAsJsonAsync(Url, request with { Email = request.Email.ToUpperInvariant() }, TestAccounts.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_ByReceptionist_Returns403()
    {
        var receptionist = await factory.CreateStaffClientAsync(StaffRole.Receptionist);

        var response = await receptionist.Client.PostAsJsonAsync(Url, ValidRequest(StaffRole.Dentist), TestAccounts.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateProfile_NewAddress_ReturnsUpdatedAccount()
    {
        var account = await factory.CreateStaffAccountAsync(StaffRole.Dentist);
        var request = new UpdateStaffProfileRequest("Ten Moi", "0987654321", new AddressDto(null, "Phuong Thao Dien", "TP Ho Chi Minh"));

        var response = await _admin.PutAsJsonAsync($"{Url}/{account.Id}", request, TestAccounts.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<StaffAccountDto>(TestAccounts.JsonOptions);
        updated!.FullName.Should().Be("Ten Moi");
        updated.Address.Should().Be(request.Address);
    }

    [Fact]
    public async Task Delete_StaffAccount_Returns204AndAccountCanNoLongerRefresh()
    {
        var staff = await factory.CreateStaffClientAsync(StaffRole.Dentist);

        var delete = await _admin.DeleteAsync($"{Url}/{staff.AccountId}");
        var refresh = await factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/refresh", new Application.Auth.RefreshTokenRequest(staff.Tokens.RefreshToken));

        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var response = await _admin.GetAsync($"{Url}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static CreateStaffAccountRequest ValidRequest(StaffRole role) =>
        new($"{Guid.NewGuid():N}@example.com", TestAccounts.Password, role, "Nha Si Test", TestAccounts.NewPhoneNumber(),
            new AddressDto("10 Tran Hung Dao", "Phuong Cau Ong Lanh", "TP Ho Chi Minh"));
}
