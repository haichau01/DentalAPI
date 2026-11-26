using DentalAPI.Application.Common.Exceptions;
using DentalAPI.Application.PatientAccounts;
using DentalAPI.Domain.Accounts;
using FluentAssertions;

namespace DentalAPI.UnitTests.Auth;

public sealed class PatientLoginServiceTests
{
    private readonly AuthTestHarness _harness = new();
    private readonly PatientLoginService _service;

    public PatientLoginServiceTests()
    {
        _service = _harness.CreatePatientLoginService();
    }

    [Theory]
    [InlineData("0900000001")]
    [InlineData("+84900000001")]
    public async Task LoginAsync_PhoneInAnyFormat_ReturnsTokens(string identifier)
    {
        _harness.AddActivePatientAccount("0900000001");

        var tokens = await _service.LoginAsync(new(identifier, AuthTestHarness.Password), CancellationToken.None);

        tokens.AccessToken.Should().NotBeEmpty();
        _harness.RefreshTokens.Tokens.Should().ContainSingle();
    }

    [Fact]
    public async Task LoginAsync_VerifiedEmail_ReturnsTokens()
    {
        var account = _harness.AddActivePatientAccount(email: "a@example.com");
        account.MarkEmailVerified();

        var tokens = await _service.LoginAsync(new("A@Example.com", AuthTestHarness.Password), CancellationToken.None);

        tokens.AccessToken.Should().NotBeEmpty();
    }

    [Fact]
    public async Task LoginAsync_UnverifiedEmail_ThrowsAuthenticationFailed()
    {
        _harness.AddActivePatientAccount(email: "a@example.com");

        var act = () => _service.LoginAsync(new("a@example.com", AuthTestHarness.Password), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task LoginAsync_PendingAccount_ThrowsAuthenticationFailedAndSpendsDummyHashTime()
    {
        var pending = new PatientAccount(
            "0900000001", null, _harness.PasswordHasher.Hash(AuthTestHarness.Password), AuthTestHarness.PendingProfile());
        _harness.PatientAccounts.Add(pending);

        var act = () => _service.LoginAsync(new("0900000001", AuthTestHarness.Password), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
        _harness.PasswordHasher.DummyVerificationCount.Should().Be(1);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_RecordsFailureAndSaves()
    {
        var account = _harness.AddActivePatientAccount();

        var act = () => _service.LoginAsync(new(account.PhoneNumber, "Wrong12345"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
        account.Lockout.FailedCount.Should().Be(1);
        _harness.UnitOfWork.SaveChangesCallCount.Should().Be(1);
        _harness.RefreshTokens.Tokens.Should().BeEmpty();
    }

    [Fact]
    public async Task LoginAsync_LockedAccountWithCorrectPassword_ThrowsAuthenticationFailed()
    {
        var account = _harness.AddActivePatientAccount();
        for (var attempt = 0; attempt < LoginLockout.MaxFailedAttempts; attempt++)
        {
            account.Lockout.RecordFailure(AuthTestHarness.UtcNow);
        }

        var act = () => _service.LoginAsync(new(account.PhoneNumber, AuthTestHarness.Password), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }
}
