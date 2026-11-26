namespace DentalAPI.Application.Auth;

/// <summary>Returned on every successful sign-in, refresh and verification.</summary>
public sealed record AuthTokensDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
