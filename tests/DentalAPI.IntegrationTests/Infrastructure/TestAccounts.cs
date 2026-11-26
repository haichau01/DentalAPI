using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DentalAPI.Application.Auth;
using DentalAPI.Application.Common.Addresses;
using DentalAPI.Application.PatientAccounts;
using DentalAPI.Application.StaffAccounts;
using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Common;
using DentalAPI.Infrastructure.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace DentalAPI.IntegrationTests.Infrastructure;

/// <summary>A signed-in test client together with the account it belongs to.</summary>
public sealed record SignedInClient(HttpClient Client, Guid AccountId, AuthTokensDto Tokens);

/// <summary>Creates fake accounts through the real API and returns clients that send their access token.</summary>
public static partial class TestAccounts
{
    public const string Password = "Passw0rd-Test";
    public const string StaffAccountsUrl = "/api/v1/staff-accounts";

    /// <summary>Matches the API's JSON settings, which write enums as strings.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<HttpClient> CreateAdminClientAsync(this DentalApiFactory factory)
    {
        var client = factory.CreateClient();
        var tokens = await PostForTokensAsync(
            client, "/api/v1/auth/staff/login", new StaffLoginRequest(DentalApiFactory.AdminEmail, DentalApiFactory.AdminPassword));
        return WithBearer(client, tokens);
    }

    /// <summary>Admin creates the account; the staff member signs in and replaces the initial password.</summary>
    public static async Task<SignedInClient> CreateStaffClientAsync(this DentalApiFactory factory, StaffRole role)
    {
        var account = await factory.CreateStaffAccountAsync(role);

        var client = factory.CreateClient();
        var firstTokens = await PostForTokensAsync(client, "/api/v1/auth/staff/login", new StaffLoginRequest(account.Email, Password));
        WithBearer(client, firstTokens);
        var tokens = await PostForTokensAsync(
            client, "/api/v1/auth/staff/change-password", new ChangePasswordRequest(Password, Password + "2"));

        return new SignedInClient(WithBearer(client, tokens), account.Id, tokens);
    }

    public static async Task<StaffAccountDto> CreateStaffAccountAsync(this DentalApiFactory factory, StaffRole role)
    {
        using var admin = await factory.CreateAdminClientAsync();
        var request = new CreateStaffAccountRequest(
            $"{Guid.NewGuid():N}@example.com", Password, role, "Nhan Vien Test", NewPhoneNumber(),
            new AddressDto("1 Le Loi", "Phuong Ben Nghe", "TP Ho Chi Minh"));

        var response = await admin.PostAsJsonAsync(StaffAccountsUrl, request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StaffAccountDto>(JsonOptions))!;
    }

    /// <summary>Registers, verifies the phone with the code from the fake SMS outbox and signs in.</summary>
    public static async Task<SignedInClient> CreatePatientClientAsync(this DentalApiFactory factory, string? phoneNumber = null)
    {
        var client = factory.CreateClient();
        var phone = phoneNumber ?? NewPhoneNumber();

        var register = await client.PostAsJsonAsync("/api/v1/auth/patients/register", NewRegistration(phone));
        register.EnsureSuccessStatusCode();

        // The API stores phones in domestic form, so the code is sent to the normalized number.
        var code = factory.ReadLatestCode(PhoneNumber.Normalize(phone));
        var tokens = await PostForTokensAsync(client, "/api/v1/auth/patients/verify-phone", new VerifyPhoneRequest(phone, code));
        var accountId = ReadSubject(tokens.AccessToken);

        return new SignedInClient(WithBearer(client, tokens), accountId, tokens);
    }

    public static RegisterPatientAccountRequest NewRegistration(string phoneNumber) =>
        new("Benh Nhan Test", new DateOnly(1995, 5, 5), phoneNumber, null, Password, new AddressDto(null, null, "Ha Noi"));

    /// <summary>Reads the verification code the fake SMS/email sender stored for the destination.</summary>
    public static string ReadLatestCode(this DentalApiFactory factory, string destination)
    {
        var message = factory.Services.GetRequiredService<InMemoryMessageOutbox>().GetLatest(destination)
            ?? throw new InvalidOperationException("No message was sent to the destination.");
        return VerificationCodePattern().Match(message).Value;
    }

    /// <summary>A random 10-digit Vietnamese mobile number so tests do not collide with each other or the seed data.</summary>
    public static string NewPhoneNumber() => "09" + Random.Shared.Next(10_000_000, 100_000_000);

    public static HttpClient WithBearer(HttpClient client, AuthTokensDto tokens)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    public static async Task<AuthTokensDto> PostForTokensAsync<TRequest>(HttpClient client, string url, TRequest request)
    {
        var response = await client.PostAsJsonAsync(url, request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokensDto>(JsonOptions))!;
    }

    private static Guid ReadSubject(string accessToken)
    {
        var payload = accessToken.Split('.')[1];
        var json = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/').PadRight((payload.Length + 3) / 4 * 4, '='));
        using var document = JsonDocument.Parse(json);
        return Guid.Parse(document.RootElement.GetProperty("sub").GetString()!);
    }

    [GeneratedRegex("[0-9]{6}")]
    private static partial Regex VerificationCodePattern();
}
