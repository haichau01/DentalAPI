using System.Net;
using System.Net.Http.Json;
using DentalAPI.Application.PatientAccounts;
using DentalAPI.Application.Patients;
using DentalAPI.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace DentalAPI.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class PatientAuthEndpointTests(DentalApiFactory factory)
{
    private const string RegisterUrl = "/api/v1/auth/patients/register";
    private const string VerifyPhoneUrl = "/api/v1/auth/patients/verify-phone";
    private const string LoginUrl = "/api/v1/auth/patients/login";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_ValidRequest_Returns202AndSendsCode()
    {
        var phone = TestAccounts.NewPhoneNumber();

        var response = await _client.PostAsJsonAsync(RegisterUrl, TestAccounts.NewRegistration(phone));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        factory.ReadLatestCode(phone).Should().MatchRegex("^[0-9]{6}$");
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400WithPasswordError()
    {
        var request = TestAccounts.NewRegistration(TestAccounts.NewPhoneNumber()) with { Password = "abc" };

        var response = await _client.PostAsJsonAsync(RegisterUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Keys.Should().Contain(nameof(RegisterPatientAccountRequest.Password));
    }

    [Fact]
    public async Task Register_PhoneOfActiveAccount_Returns409()
    {
        var phone = TestAccounts.NewPhoneNumber();
        await factory.CreatePatientClientAsync(phone);

        var response = await _client.PostAsJsonAsync(RegisterUrl, TestAccounts.NewRegistration(phone));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task VerifyPhone_WrongCode_Returns400AndAccountCannotSignIn()
    {
        var phone = TestAccounts.NewPhoneNumber();
        await _client.PostAsJsonAsync(RegisterUrl, TestAccounts.NewRegistration(phone));
        var wrongCode = factory.ReadLatestCode(phone) == "000000" ? "111111" : "000000";

        var verify = await _client.PostAsJsonAsync(VerifyPhoneUrl, new VerifyPhoneRequest(phone, wrongCode));
        var login = await _client.PostAsJsonAsync(LoginUrl, new PatientLoginRequest(phone, TestAccounts.Password));

        verify.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task VerifyPhone_PhoneOfExistingPatientRecord_LinksAccountToThatRecord()
    {
        var phone = TestAccounts.NewPhoneNumber();
        var existing = await CreatePatientRecordAsReceptionAsync(phone);

        var patient = await factory.CreatePatientClientAsync("+84" + phone[1..]);
        var profile = await patient.Client.GetFromJsonAsync<PatientProfileDto>("/api/v1/me/profile");

        profile!.Id.Should().Be(existing.Id);
        profile.FullName.Should().Be("Ho So Le Tan");
    }

    [Fact]
    public async Task Login_ByPhone_Returns200WithTokens()
    {
        var phone = TestAccounts.NewPhoneNumber();
        await factory.CreatePatientClientAsync(phone);

        var response = await _client.PostAsJsonAsync(LoginUrl, new PatientLoginRequest(phone, TestAccounts.Password));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401ProblemDetails()
    {
        var phone = TestAccounts.NewPhoneNumber();
        await factory.CreatePatientClientAsync(phone);

        var response = await _client.PostAsJsonAsync(LoginUrl, new PatientLoginRequest(phone, "Wrong-Passw0rd"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Login_ByEmail_WorksOnlyAfterEmailIsVerified()
    {
        var patient = await factory.CreatePatientClientAsync();
        var email = $"{Guid.NewGuid():N}@example.com";
        var sendCode = await patient.Client.PostAsJsonAsync("/api/v1/me/email/send-code", new SendEmailVerificationCodeRequest(email));

        var beforeVerify = await _client.PostAsJsonAsync(LoginUrl, new PatientLoginRequest(email, TestAccounts.Password));
        var verify = await patient.Client.PostAsJsonAsync("/api/v1/me/email/verify", new VerifyEmailRequest(factory.ReadLatestCode(email)));
        var afterVerify = await _client.PostAsJsonAsync(LoginUrl, new PatientLoginRequest(email, TestAccounts.Password));

        sendCode.StatusCode.Should().Be(HttpStatusCode.Accepted);
        beforeVerify.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        verify.StatusCode.Should().Be(HttpStatusCode.NoContent);
        afterVerify.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<PatientDto> CreatePatientRecordAsReceptionAsync(string phone)
    {
        using var admin = await factory.CreateAdminClientAsync();
        var request = new SavePatientRequest("Ho So Le Tan", new DateOnly(1970, 2, 2), phone, null, "Di ung penicillin");
        var response = await admin.PostAsJsonAsync("/api/v1/patients", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PatientDto>())!;
    }
}
