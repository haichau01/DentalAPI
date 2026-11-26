using DentalAPI.Application.Auth;
using DentalAPI.Domain.Accounts;
using DentalAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalAPI.Infrastructure.Repositories;

public sealed class RefreshTokenRepository(DentalDbContext dbContext) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> ListUnrevokedByAccountAsync(Guid accountId, CancellationToken cancellationToken) =>
        await dbContext.RefreshTokens
            .Where(token => token.AccountId == accountId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

    public void Add(RefreshToken refreshToken) => dbContext.RefreshTokens.Add(refreshToken);
}

public sealed class VerificationCodeRepository(DentalDbContext dbContext) : IVerificationCodeRepository
{
    public Task<VerificationCode?> GetLatestAsync(VerificationChannel channel, string destination, CancellationToken cancellationToken) =>
        dbContext.VerificationCodes
            .Where(code => code.Channel == channel && code.Destination == destination)
            .OrderByDescending(code => code.SentAt)
            .FirstOrDefaultAsync(cancellationToken);

    // Hard delete on purpose: expired codes have no history value and their destination is a phone or email.
    // IgnoreQueryFilters so soft-deleted codes are removed too.
    public Task<int> DeleteSentBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken) =>
        dbContext.VerificationCodes
            .IgnoreQueryFilters()
            .Where(code => code.SentAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

    public void Add(VerificationCode verificationCode) => dbContext.VerificationCodes.Add(verificationCode);
}
