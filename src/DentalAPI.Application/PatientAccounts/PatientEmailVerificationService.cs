using DentalAPI.Application.Auth;
using DentalAPI.Application.Common.Exceptions;
using DentalAPI.Application.Common.Persistence;
using DentalAPI.Application.Common.Security;
using DentalAPI.Domain.Accounts;
using FluentValidation;

namespace DentalAPI.Application.PatientAccounts;

/// <summary>Lets a signed-in patient add an email and verify it so it can be used to sign in.</summary>
public interface IPatientEmailVerificationService
{
    /// <summary>Sets the account's email (unverified) and sends a code to it.</summary>
    /// <exception cref="EmailAlreadyInUseException">Another account signs in with this email.</exception>
    /// <exception cref="VerificationCodeCooldownException">A code was sent too recently.</exception>
    Task SendCodeAsync(SendEmailVerificationCodeRequest request, CancellationToken cancellationToken);

    /// <exception cref="InvalidVerificationCodeException">The code is invalid or no email is pending.</exception>
    /// <exception cref="EmailAlreadyInUseException">Another account verified this email first.</exception>
    Task VerifyAsync(VerifyEmailRequest request, CancellationToken cancellationToken);
}

public sealed class PatientEmailVerificationService(
    ICurrentUser currentUser,
    IPatientAccountRepository accountRepository,
    VerificationCodeService verificationCodeService,
    IUnitOfWork unitOfWork,
    IValidator<SendEmailVerificationCodeRequest> sendValidator,
    IValidator<VerifyEmailRequest> verifyValidator) : IPatientEmailVerificationService
{
    public async Task SendCodeAsync(SendEmailVerificationCodeRequest request, CancellationToken cancellationToken)
    {
        await sendValidator.ValidateAndThrowAsync(request, cancellationToken);

        var account = await GetCurrentAccountAsync(cancellationToken);
        var email = PatientAccount.NormalizeEmail(request.Email)!;
        await EnsureEmailIsFreeAsync(email, account.Id, cancellationToken);

        account.ChangeEmail(email);
        await verificationCodeService.SendAsync(VerificationChannel.Email, email, cancellationToken);
    }

    public async Task VerifyAsync(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        await verifyValidator.ValidateAndThrowAsync(request, cancellationToken);

        var account = await GetCurrentAccountAsync(cancellationToken);
        if (account.Email is null)
        {
            throw new InvalidVerificationCodeException();
        }

        await verificationCodeService.VerifyAsync(VerificationChannel.Email, account.Email, request.Code, cancellationToken);
        await EnsureEmailIsFreeAsync(account.Email, account.Id, cancellationToken);

        account.MarkEmailVerified();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<PatientAccount> GetCurrentAccountAsync(CancellationToken cancellationToken)
    {
        var accountId = currentUser.GetRequiredAccountId(AccountType.Patient);
        return await accountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new ForbiddenException();
    }

    private async Task EnsureEmailIsFreeAsync(string email, Guid accountId, CancellationToken cancellationToken)
    {
        if (await accountRepository.IsVerifiedEmailInUseAsync(email, accountId, cancellationToken))
        {
            throw new EmailAlreadyInUseException();
        }
    }
}
