namespace DentalAPI.Domain.Accounts;

/// <summary>An account that signs in with a password and is protected by the same lockout rule.</summary>
public interface IPasswordCredential
{
    string PasswordHash { get; }

    LoginLockout Lockout { get; }
}
