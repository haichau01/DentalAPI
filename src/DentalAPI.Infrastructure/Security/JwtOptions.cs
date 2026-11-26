using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DentalAPI.Infrastructure.Security;

/// <summary>Settings from the "Jwt" section. The signing key comes from user-secrets or an environment variable, never from committed config.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 needs a key of at least 256 bits.</summary>
    public const int MinSigningKeyBytes = 32;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Short, because access tokens cannot be revoked before they expire.</summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));

    public bool HasValidSigningKey() => Encoding.UTF8.GetByteCount(SigningKey) >= MinSigningKeyBytes;
}
