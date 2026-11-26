using System.Net;
using System.Net.Http.Json;
using DentalAPI.Application.PatientAccounts;
using DentalAPI.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace DentalAPI.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class RegistrationCleanupTests(DentalApiFactory factory)
{
    [Fact]
    public async Task DeletePendingNotUpdatedSince_RemovesPendingRegistrationButKeepsActiveAccounts()
    {
        using var client = factory.CreateClient();
        var pendingPhone = TestAccounts.NewPhoneNumber();
        await client.PostAsJsonAsync("/api/v1/auth/patients/register", TestAccounts.NewRegistration(pendingPhone));
        var activePhone = TestAccounts.NewPhoneNumber();
        await factory.CreatePatientClientAsync(activePhone);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IPatientAccountRepository>();
            var deleted = await repository.DeletePendingNotUpdatedSinceAsync(DateTimeOffset.UtcNow.AddMinutes(1), CancellationToken.None);
            deleted.Should().BeGreaterThanOrEqualTo(1);
        }

        var verifyDeleted = await client.PostAsJsonAsync(
            "/api/v1/auth/patients/verify-phone", new VerifyPhoneRequest(pendingPhone, factory.ReadLatestCode(pendingPhone)));
        var activeLogin = await client.PostAsJsonAsync(
            "/api/v1/auth/patients/login", new PatientLoginRequest(activePhone, TestAccounts.Password));

        verifyDeleted.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        activeLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
