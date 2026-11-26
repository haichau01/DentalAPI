namespace DentalAPI.Domain.Accounts;

/// <summary>
/// Temporarily blocks sign-in for everyone after many wrong passwords, as a last guard against guessing spread
/// over many IP addresses. A single client is already stopped much earlier by the per-client login throttle,
/// so the account-wide limit is set high: a low one would let anyone lock a patient out just by knowing their phone.
/// </summary>
public sealed class LoginLockout
{
    public const int MaxFailedAttempts = 20;
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
