using DentalAPI.Application.Auth;
using DentalAPI.Application.Common.Exceptions;
using DentalAPI.Domain.Accounts;
using FluentAssertions;

namespace DentalAPI.UnitTests.Auth;

public sealed class RefreshTokenServiceTests
{
    private readonly AuthTestHarness _harness = new();
    private readonly RefreshTokenService _service;

    public RefreshTokenServiceTests()
    {
        _service = _harness.CreateRefreshTokenService();
    }

    [Fact]
    public async Task RefreshAsync_ActiveToken_RevokesItAndReturnsNewPair()
    {
        var account = _harness.AddActivePatientAccount();
        var issued = _harness.CreateTokenPairIssuer().Issue(Application.Common.Security.AccessTokenSubject.ForPatient(account));

        var tokens = await _service.RefreshAsync(new(issued.Tokens.RefreshToken), CancellationToken.None);

        tokens.RefreshToken.Should().NotBe(issued.Tokens.RefreshToken);
        var oldToken = _harness.RefreshTokens.Tokens.Single(token => token.Id == issued.RefreshTokenId);
        oldToken.IsRevoked.Should().BeTrue();
        oldToken.ReplacedByTokenId.Should().NotBeNull();
        _harness.RefreshTokens.Tokens.Should().HaveCount(2);
    }

    [Fact]
    public async Task RefreshAsync_ReusedRotatedToken_RevokesEveryTokenOfTheAccount()
    {
        var account = _harness.AddActivePatientAccount();
        var first = _harness.CreateTokenPairIssuer().Issue(Application.Common.Security.AccessTokenSubject.ForPatient(account));
        await _service.RefreshAsync(new(first.Tokens.RefreshToken), CancellationToken.None);

        var act = () => _service.RefreshAsync(new(first.Tokens.RefreshToken), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
        _harness.RefreshTokens.Tokens.Should().OnlyContain(token => token.IsRevoked);
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_ThrowsAuthenticationFailed()
    {
        var account = _harness.AddActivePatientAccount();
        var expired = new RefreshToken(account.Id, AccountType.Patient, "hash:old", AuthTestHarness.UtcNow.AddSeconds(-1));
        _harness.RefreshTokens.Add(expired);

        var act = () => _service.RefreshAsync(new("old"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task RefreshAsync_UnknownToken_ThrowsAuthenticationFailed()
    {
        var act = () => _service.RefreshAsync(new("never-issued"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task LogoutAsync_TokenOfAnotherAccount_LeavesItActive()
    {
        var account = _harness.AddActivePatientAccount();
        var issued = _harness.CreateTokenPairIssuer().Issue(Application.Common.Security.AccessTokenSubject.ForPatient(account));
        _harness.CurrentUser.AccountId = Guid.NewGuid();

        await _service.LogoutAsync(new(issued.Tokens.RefreshToken), CancellationToken.None);

        _harness.RefreshTokens.Tokens.Should().OnlyContain(token => !token.IsRevoked);
    }
}
