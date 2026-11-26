using System.Net;
using System.Net.Http.Json;
using DentalAPI.Application.Auth;
using DentalAPI.Application.StaffAccounts;
using DentalAPI.Domain.Accounts;
using DentalAPI.IntegrationTests.Infrastructure;
using FluentAssertions;
using ILoginAttemptThrottle = DentalAPI.Application.Auth.ILoginAttemptThrottle;

namespace DentalAPI.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class StaffAuthEndpointTests(DentalApiFactory factory)
{
    private const string LoginUrl = "/api/v1/auth/staff/login";
    private const string RefreshUrl = "/api/v1/auth/refresh";
    private const string LogoutUrl = "/api/v1/auth/logout";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_BootstrapAdmin_Returns200WithTokens()
    {
        var response = await _client.PostAsJsonAsync(
            LoginUrl, new StaffLoginRequest(DentalApiFactory.AdminEmail, DentalApiFactory.AdminPassword));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokensDto>();
        tokens!.AccessToken.Should().NotBeEmpty();
        tokens.RefreshToken.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        // Uses a throwaway account: failed attempts on the shared admin would lock it for the other tests.
        var account = await factory.CreateStaffAccountAsync(StaffRole.Dentist);

        var response = await _client.PostAsJsonAsync(LoginUrl, new StaffLoginRequest(account.Email, "Wrong-Passw0rd"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_CorrectPasswordAfterTooManyWrongOnesFromSameClient_Returns401()
    {
        var account = await factory.CreateStaffAccountAsync(StaffRole.Dentist);
        for (var attempt = 0; attempt < ILoginAttemptThrottle.MaxFailuresPerClient; attempt++)
        {
            await _client.PostAsJsonAsync(LoginUrl, new StaffLoginRequest(account.Email, "Wrong-Passw0rd"));
        }

        var response = await _client.PostAsJsonAsync(LoginUrl, new StaffLoginRequest(account.Email, TestAccounts.Password));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NewStaff_BeforeAndAfterChangingInitialPassword_IsForbiddenThenAllowed()
    {
        var account = await factory.CreateStaffAccountAsync(StaffRole.Receptionist);
        var firstTokens = await TestAccounts.PostForTokensAsync(_client, LoginUrl, new StaffLoginRequest(account.Email, TestAccounts.Password));
        TestAccounts.WithBearer(_client, firstTokens);

        var beforeChange = await _client.GetAsync("/api/v1/patients");
        var newTokens = await TestAccounts.PostForTokensAsync(
            _client, "/api/v1/auth/staff/change-password", new ChangePasswordRequest(TestAccounts.Password, "N3w-Passw0rd"));
        TestAccounts.WithBearer(_client, newTokens);
        var afterChange = await _client.GetAsync("/api/v1/patients");

        beforeChange.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        afterChange.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_ValidToken_Returns200AndOldTokenStopsWorking()
    {
        var staff = await factory.CreateStaffClientAsync(StaffRole.Dentist);

        var first = await _client.PostAsJsonAsync(RefreshUrl, new RefreshTokenRequest(staff.Tokens.RefreshToken));
        var reuse = await _client.PostAsJsonAsync(RefreshUrl, new RefreshTokenRequest(staff.Tokens.RefreshToken));

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ReusedToken_RevokesTheNewTokenToo()
    {
        var staff = await factory.CreateStaffClientAsync(StaffRole.Dentist);
        var rotated = await TestAccounts.PostForTokensAsync(_client, RefreshUrl, new RefreshTokenRequest(staff.Tokens.RefreshToken));
        await _client.PostAsJsonAsync(RefreshUrl, new RefreshTokenRequest(staff.Tokens.RefreshToken));

        var response = await _client.PostAsJsonAsync(RefreshUrl, new RefreshTokenRequest(rotated.RefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_Returns204AndRefreshTokenStopsWorking()
    {
        var patient = await factory.CreatePatientClientAsync();

        var logout = await patient.Client.PostAsJsonAsync(LogoutUrl, new RefreshTokenRequest(patient.Tokens.RefreshToken));
        var refresh = await _client.PostAsJsonAsync(RefreshUrl, new RefreshTokenRequest(patient.Tokens.RefreshToken));

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithoutAccessToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync(LogoutUrl, new RefreshTokenRequest("anything"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
