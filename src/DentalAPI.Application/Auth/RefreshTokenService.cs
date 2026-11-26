using DentalAPI.Application.Common.Exceptions;
using DentalAPI.Application.Common.Persistence;
using DentalAPI.Application.Common.Security;
using DentalAPI.Application.PatientAccounts;
using DentalAPI.Application.StaffAccounts;
using DentalAPI.Domain.Accounts;
using FluentValidation;

namespace DentalAPI.Application.Auth;

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    private const int MaxTokenLength = 512;

    public RefreshTokenRequestValidator()
    {
        RuleFor(request => request.RefreshToken).NotEmpty().MaximumLength(MaxTokenLength);
    }
}

/// <summary>Exchanges and revokes refresh tokens for both patient and staff accounts.</summary>
public interface IRefreshTokenService
{
    /// <summary>Rotates the refresh token: the presented one is revoked and a new pair is returned.</summary>
    /// <exception cref="AuthenticationFailedException">The token is unknown, expired, revoked, or its account is gone.</exception>
    Task<AuthTokensDto> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    /// <summary>Revokes the caller's refresh token. Unknown tokens are ignored so logout is idempotent.</summary>
    Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    /// <summary>Revokes every refresh token of an account, e.g. after a password change or account deletion. Does not save.</summary>
    Task RevokeAllAsync(Guid accountId, CancellationToken cancellationToken);
}

public sealed class RefreshTokenService(
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenFactory refreshTokenFactory,
    IStaffAccountRepository staffAccountRepository,
    IPatientAccountRepository patientAccountRepository,
    TokenPairIssuer tokenPairIssuer,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IValidator<RefreshTokenRequest> requestValidator,
    TimeProvider timeProvider) : IRefreshTokenService
{
    public async Task<AuthTokensDto> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await requestValidator.ValidateAndThrowAsync(request, cancellationToken);

        var now = timeProvider.GetUtcNow();
        var presented = await refreshTokenRepository.GetByHashAsync(refreshTokenFactory.Hash(request.RefreshToken), cancellationToken)
            ?? throw new AuthenticationFailedException();

        if (presented.IsRevoked)
        {
            // A rotated token is only presented again if someone kept a copy of it,
            // so treat it as stolen and sign the account out everywhere.
            await RevokeAllAsync(presented.AccountId, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new AuthenticationFailedException();
        }

        var subject = await FindSubjectAsync(presented, cancellationToken);
        if (presented.IsExpired(now) || subject is null)
        {
            throw new AuthenticationFailedException();
        }

        var issued = tokenPairIssuer.Issue(subject);
        presented.Revoke(now, issued.RefreshTokenId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return issued.Tokens;
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await requestValidator.ValidateAndThrowAsync(request, cancellationToken);

        var token = await refreshTokenRepository.GetByHashAsync(refreshTokenFactory.Hash(request.RefreshToken), cancellationToken);
        if (token is null || token.AccountId != currentUser.AccountId)
        {
            return;
        }

        token.Revoke(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var tokens = await refreshTokenRepository.ListUnrevokedByAccountAsync(accountId, cancellationToken);
        foreach (var token in tokens)
        {
            token.Revoke(now);
        }
    }

    private async Task<AccessTokenSubject?> FindSubjectAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        if (token.AccountType == AccountType.Staff)
        {
            var staffAccount = await staffAccountRepository.GetByIdAsync(token.AccountId, cancellationToken);
            return staffAccount is null ? null : AccessTokenSubject.ForStaff(staffAccount);
        }

        var patientAccount = await patientAccountRepository.GetByIdAsync(token.AccountId, cancellationToken);
        return patientAccount is { IsActive: true } ? AccessTokenSubject.ForPatient(patientAccount) : null;
    }
}
