namespace DentalAPI.Application.Auth;

/// <summary>
/// Counts failed sign-ins per (account identifier, client address). It stops one client from guessing a password
/// without blocking the real owner, who signs in from another address.
/// </summary>
public interface ILoginAttemptThrottle
{
    public const int MaxFailuresPerClient = 5;

    public static readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(15);

    bool IsBlocked(string identifier, string clientAddress);

    void RecordFailure(string identifier, string clientAddress);

    void Reset(string identifier, string clientAddress);
}
