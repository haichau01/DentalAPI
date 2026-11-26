using DentalAPI.Domain.Common;

namespace DentalAPI.Domain.Accounts;

/// <summary>
/// A long-lived token exchanged for a new access token. Each use replaces it with a new one (rotation),
/// so a stolen token stops working as soon as the real owner refreshes. Only the hash is stored.
/// </summary>
public sealed class RefreshToken : Entity
{
    public const int TokenHashMaxLength = 128;

    // Required by EF Core to materialize entities from the database.
    private RefreshToken()
    {
    }

    public RefreshToken(Guid accountId, AccountType accountType, string tokenHash, DateTimeOffset expiresAt)
    {
        AccountId = accountId;
        AccountType = accountType;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public Guid AccountId { get; private set; }

    public AccountType AccountType { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public void Revoke(DateTimeOffset now, Guid? replacedByTokenId = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAt = now;
        ReplacedByTokenId = replacedByTokenId;
    }
}
