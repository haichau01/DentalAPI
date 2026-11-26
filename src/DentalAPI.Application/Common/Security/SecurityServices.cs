namespace DentalAPI.Application.Common.Security;

/// <summary>Hashes and checks passwords with a slow, salted algorithm.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);

    /// <summary>
    /// Spends the same time as <see cref="Verify"/> without a real account, so response time
    /// does not reveal whether a phone number or email is registered.
    /// </summary>
    void VerifyAgainstDummy(string password);
}

/// <summary>Creates signed, short-lived access tokens.</summary>
public interface IAccessTokenIssuer
{
    AccessToken Issue(AccessTokenSubject subject);
}

/// <summary>A new refresh token: the plain value goes to the client once, only the hash is stored.</summary>
public sealed record NewRefreshToken(string PlainText, string Hash, DateTimeOffset ExpiresAt);

/// <summary>Creates random refresh tokens and hashes presented ones for lookup.</summary>
public interface IRefreshTokenFactory
{
    NewRefreshToken Create();

    string Hash(string plainText);
}

/// <summary>Creates random one-time verification codes and hashes them for storage.</summary>
public interface IVerificationCodeGenerator
{
    string Generate();

    string Hash(string code);
}
