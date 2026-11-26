using DentalAPI.Application.PatientAccounts;
using DentalAPI.Domain.Accounts;
using FluentAssertions;

namespace DentalAPI.UnitTests.Auth;

public sealed class PatientEmailVerificationServiceTests
{
    private readonly AuthTestHarness _harness = new();
    private readonly PatientEmailVerificationService _service;

    public PatientEmailVerificationServiceTests()
    {
        _service = _harness.CreateEmailVerificationService();
    }

    [Fact]
    public async Task SendCodeAsync_FreeEmail_SetsUnverifiedEmailAndSendsCode()
    {
        var account = SignInAsNewPatient();

        await _service.SendCodeAsync(new("New@Example.com"), CancellationToken.None);

        account.Email.Should().Be("new@example.com");
        account.IsEmailVerified.Should().BeFalse();
        _harness.Messages.Sent.Should().ContainSingle(message => message.Destination == "new@example.com");
    }

    [Fact]
    public async Task SendCodeAsync_EmailVerifiedByAnotherAccount_SucceedsWithoutChangingOrSending()
    {
        var other = _harness.AddActivePatientAccount("0911111111", "taken@example.com");
        other.MarkEmailVerified();
        var account = SignInAsNewPatient();

        await _service.SendCodeAsync(new("taken@example.com"), CancellationToken.None);

        account.Email.Should().BeNull();
        _harness.Messages.Sent.Should().BeEmpty();
    }

    private PatientAccount SignInAsNewPatient()
    {
        var account = _harness.AddActivePatientAccount("0922222222");
        _harness.CurrentUser.AccountId = account.Id;
        _harness.CurrentUser.AccountType = AccountType.Patient;
        _harness.CurrentUser.PatientId = account.PatientId;
        return account;
    }
}

public sealed class ExpiredRegistrationCleanerTests
{
    private readonly AuthTestHarness _harness = new();

    [Fact]
    public async Task PurgeAsync_DeletesOnlyDataOlderThanPendingLifetime()
    {
        var lifetime = PatientAccount.PendingRegistrationLifetime;
        _harness.VerificationCodes.Add(new VerificationCode(VerificationChannel.Sms, "0900000001", "h", AuthTestHarness.UtcNow - lifetime - TimeSpan.FromMinutes(1)));
        _harness.VerificationCodes.Add(new VerificationCode(VerificationChannel.Sms, "0900000002", "h", AuthTestHarness.UtcNow - lifetime + TimeSpan.FromMinutes(1)));
        var cleaner = new ExpiredRegistrationCleaner(_harness.PatientAccounts, _harness.VerificationCodes, _harness.TimeProvider);

        var result = await cleaner.PurgeAsync(CancellationToken.None);

        result.DeletedVerificationCodes.Should().Be(1);
        _harness.VerificationCodes.Codes.Should().ContainSingle(code => code.Destination == "0900000002");
        _harness.PatientAccounts.LastPurgeCutoff.Should().Be(AuthTestHarness.UtcNow - lifetime);
    }

    [Fact]
    public void RestartRegistration_ActiveAccount_ThrowsInvalidOperation()
    {
        var account = _harness.AddActivePatientAccount();

        var act = () => account.RestartRegistration(null, "hash", AuthTestHarness.PendingProfile());

        act.Should().Throw<InvalidOperationException>();
    }
}
