using DentalAPI.Application.Auth;
using DentalAPI.Application.Common.Exceptions;
using DentalAPI.Application.Common.Persistence;
using DentalAPI.Application.Common.Security;
using DentalAPI.Domain.Accounts;
using FluentValidation;

namespace DentalAPI.Application.StaffAccounts;

/// <summary>Staff sign-in by email, and password changes.</summary>
public interface IStaffLoginService
{
    /// <exception cref="AuthenticationFailedException">Unknown or locked account, or wrong password.</exception>
    Task<AuthTokensDto> LoginAsync(StaffLoginRequest request, CancellationToken cancellationToken);

    /// <summary>Replaces the password, signs out every other session and returns fresh tokens without the change-required flag.</summary>
    /// <exception cref="AuthenticationFailedException">The current password is wrong.</exception>
    Task<AuthTokensDto> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken);
}

public sealed class StaffLoginService(
    IStaffAccountRepository accountRepository,
    PasswordCredentialChecker credentialChecker,
    IPasswordHasher passwordHasher,
    TokenPairIssuer tokenPairIssuer,
    IRefreshTokenService refreshTokenService,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IValidator<StaffLoginRequest> loginValidator,
    IValidator<ChangePasswordRequest> changePasswordValidator) : IStaffLoginService
{
    public async Task<AuthTokensDto> LoginAsync(StaffLoginRequest request, CancellationToken cancellationToken)
    {
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var email = StaffAccount.NormalizeEmail(request.Email);
        var account = await accountRepository.GetByEmailAsync(email, cancellationToken);
        await credentialChecker.EnsureValidAsync(email, account, request.Password, cancellationToken);

        var issued = tokenPairIssuer.Issue(AccessTokenSubject.ForStaff(account!));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return issued.Tokens;
    }

    public async Task<AuthTokensDto> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await changePasswordValidator.ValidateAndThrowAsync(request, cancellationToken);

        var accountId = currentUser.GetRequiredAccountId(AccountType.Staff);
        var account = await accountRepository.GetByIdAsync(accountId, cancellationToken);
        await credentialChecker.EnsureValidAsync(accountId.ToString(), account, request.CurrentPassword, cancellationToken);

        account!.ChangePassword(passwordHasher.Hash(request.NewPassword));
        await refreshTokenService.RevokeAllAsync(account.Id, cancellationToken);
        var issued = tokenPairIssuer.Issue(AccessTokenSubject.ForStaff(account));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return issued.Tokens;
    }
}
