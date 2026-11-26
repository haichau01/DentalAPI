using DentalAPI.Domain.Accounts;

namespace DentalAPI.Application.Auth;

/// <summary>Loads and stores refresh tokens. Returned tokens are tracked so changes are saved by the unit of work.</summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>Returns the account's tokens that are not revoked yet, including expired ones.</summary>
    Task<IReadOnlyList<RefreshToken>> ListUnrevokedByAccountAsync(Guid accountId, CancellationToken cancellationToken);

    void Add(RefreshToken refreshToken);
}

/// <summary>Loads and stores one-time verification codes.</summary>
public interface IVerificationCodeRepository
{
    /// <summary>Returns the most recently sent code for the destination, used or not.</summary>
    Task<VerificationCode?> GetLatestAsync(VerificationChannel channel, string destination, CancellationToken cancellationToken);

    void Add(VerificationCode verificationCode);
}
