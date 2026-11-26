using DentalAPI.Domain.Common;

namespace DentalAPI.Domain.Accounts;

public enum VerificationChannel
{
    Sms,
    Email,
}

/// <summary>A one-time code sent to a phone or email to prove the person owns it. Only the hash is stored.</summary>
public sealed class VerificationCode : Entity
{
    public const int DestinationMaxLength = 256;
    public const int CodeHashMaxLength = 128;
    public const int CodeLength = 6;

    /// <summary>A 6-digit code can be guessed with 1,000,000 tries, so attempts per code are capped.</summary>
    public const int MaxFailedAttempts = 5;

    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    /// <summary>Minimum wait before another code is sent to the same destination, to stop SMS flooding.</summary>
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

    // Required by EF Core to materialize entities from the database.
    private VerificationCode()
    {
    }

    public VerificationCode(VerificationChannel channel, string destination, string codeHash, DateTimeOffset now)
    {
        Channel = channel;
        Destination = destination;
        CodeHash = codeHash;
        SentAt = now;
        ExpiresAt = now + Lifetime;
    }

    public VerificationChannel Channel { get; private set; }

    public string Destination { get; private set; } = string.Empty;

    public string CodeHash { get; private set; } = string.Empty;

    public DateTimeOffset SentAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public int FailedAttempts { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public bool CanResend(DateTimeOffset now) => now >= SentAt + ResendCooldown;

    /// <summary>Marks the code as used when it matches and is still usable; otherwise counts a failed attempt.</summary>
    public bool TryConsume(string codeHash, DateTimeOffset now)
    {
        if (ConsumedAt is not null || now >= ExpiresAt || FailedAttempts >= MaxFailedAttempts)
        {
            return false;
        }

        if (!string.Equals(CodeHash, codeHash, StringComparison.Ordinal))
        {
            FailedAttempts++;
            return false;
        }

        ConsumedAt = now;
        return true;
    }
}
