namespace DentalAPI.Domain.Accounts;

/// <summary>
/// Temporarily blocks sign-in after repeated wrong passwords. The login endpoints are public,
/// so without this an attacker could guess passwords at the rate limit indefinitely.
/// </summary>
public sealed class LoginLockout
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan Duration = TimeSpan.FromMinutes(15);

    public int FailedCount { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    public bool IsLocked(DateTimeOffset now) => LockedUntil > now;

    public void RecordFailure(DateTimeOffset now)
    {
        FailedCount++;
        if (FailedCount < MaxFailedAttempts)
        {
            return;
        }

        LockedUntil = now + Duration;
        FailedCount = 0;
    }

    public void Reset()
    {
        FailedCount = 0;
        LockedUntil = null;
    }
}
