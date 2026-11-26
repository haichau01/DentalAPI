using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using DentalAPI.Application.Common.Security;
using DentalAPI.Domain.Accounts;
using Microsoft.Extensions.Options;

namespace DentalAPI.Infrastructure.Security;

/// <summary>
/// Plain SHA-256 is enough for refresh tokens because they are long random values, not guessable passwords.
/// Verification codes are short, but their attempt cap and 5-minute lifetime limit guessing, not the hash.
/// </summary>
internal static class Sha256Hash
{
    public static string Compute(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

public sealed class RefreshTokenFactory(IOptions<JwtOptions> options, TimeProvider timeProvider) : IRefreshTokenFactory
{
    private const int TokenByteLength = 64;

    public NewRefreshToken Create()
    {
        var plainText = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenByteLength));
        var expiresAt = timeProvider.GetUtcNow() + options.Value.RefreshTokenLifetime;
        return new NewRefreshToken(plainText, Hash(plainText), expiresAt);
    }

    public string Hash(string plainText) => Sha256Hash.Compute(plainText);
}

public sealed class VerificationCodeGenerator : IVerificationCodeGenerator
{
    private static readonly int _exclusiveUpperBound = (int)Math.Pow(10, VerificationCode.CodeLength);
    private static readonly string _format = $"D{VerificationCode.CodeLength}";

    public string Generate() => RandomNumberGenerator.GetInt32(0, _exclusiveUpperBound).ToString(_format);

    public string Hash(string code) => Sha256Hash.Compute(code);
}
