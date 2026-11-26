using DentalAPI.Application.Common.Security;
using Microsoft.AspNetCore.Identity;

namespace DentalAPI.Infrastructure.Security;

/// <summary>Uses ASP.NET Core Identity's PBKDF2 hasher without the rest of Identity.</summary>
public sealed class AspNetPasswordHasher : IPasswordHasher
{
    // The hasher ignores the user argument; a shared placeholder satisfies its signature.
    private static readonly object _anyUser = new();
    private static readonly PasswordHasher<object> _hasher = new();
    private static readonly Lazy<string> _dummyHash = new(() => _hasher.HashPassword(_anyUser, Guid.NewGuid().ToString()));

    public string Hash(string password) => _hasher.HashPassword(_anyUser, password);

    public bool Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(_anyUser, passwordHash, password) != PasswordVerificationResult.Failed;

    public void VerifyAgainstDummy(string password) => Verify(_dummyHash.Value, password);
}
