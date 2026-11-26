using DentalAPI.Application.Common.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DentalAPI.Infrastructure.Security;

public sealed class JwtAccessTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider) : IAccessTokenIssuer
{
    private const string TrueClaimValue = "true";

    private readonly JsonWebTokenHandler _tokenHandler = new();

    public AccessToken Issue(AccessTokenSubject subject)
    {
        var jwtOptions = options.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now + jwtOptions.AccessTokenLifetime;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtOptions.Issuer,
            Audience = jwtOptions.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = BuildClaims(subject),
            SigningCredentials = new SigningCredentials(jwtOptions.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(_tokenHandler.CreateToken(descriptor), expiresAt);
    }

    private static Dictionary<string, object> BuildClaims(AccessTokenSubject subject)
    {
        var claims = new Dictionary<string, object>
        {
            [AccessTokenClaims.Subject] = subject.AccountId.ToString(),
            [AccessTokenClaims.TokenId] = Guid.NewGuid().ToString(),
            [AccessTokenClaims.AccountType] = subject.AccountType.ToString(),
            [AccessTokenClaims.Role] = subject.Role,
        };

        if (subject.PatientId is { } patientId)
        {
            claims[AccessTokenClaims.PatientId] = patientId.ToString();
        }

        if (subject.IsPasswordChangeRequired)
        {
            claims[AccessTokenClaims.PasswordChangeRequired] = TrueClaimValue;
        }

        return claims;
    }
}
