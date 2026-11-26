using DentalAPI.Application.Common.Security;
using DentalAPI.Domain.Accounts;

namespace DentalAPI.Application.Auth;

/// <summary>The tokens handed to the client, plus the id of the stored refresh token so a rotated token can point to it.</summary>
public sealed record IssuedTokens(AuthTokensDto Tokens, Guid RefreshTokenId);

/// <summary>
/// Issues an access token and a refresh token for an account and stages the refresh token for saving.
/// The caller saves, so the tokens are stored in the same transaction as the sign-in it belongs to.
/// </summary>
public sealed class TokenPairIssuer(
    IAccessTokenIssuer accessTokenIssuer,
    IRefreshTokenFactory refreshTokenFactory,
    IRefreshTokenRepository refreshTokenRepository)
{
    public IssuedTokens Issue(AccessTokenSubject subject)
    {
        var accessToken = accessTokenIssuer.Issue(subject);
        var newRefreshToken = refreshTokenFactory.Create();

        var refreshToken = new RefreshToken(subject.AccountId, subject.AccountType, newRefreshToken.Hash, newRefreshToken.ExpiresAt);
        refreshTokenRepository.Add(refreshToken);

        var tokens = new AuthTokensDto(accessToken.Value, accessToken.ExpiresAt, newRefreshToken.PlainText, newRefreshToken.ExpiresAt);
        return new IssuedTokens(tokens, refreshToken.Id);
    }
}
