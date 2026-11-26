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
    /// <summary>
    /// Sets the account's email (unverified) and sends a code to it. When another account already signs in with
    /// the email, nothing is changed or sent but the call still succeeds, so it cannot be used to find out
    /// which emails are registered; verifying then fails with an invalid code.
    /// </summary>
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
        if (await accountRepository.IsVerifiedEmailInUseAsync(email, account.Id, cancellationToken))
        {
            return;
        }

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

        // The code proves ownership, so revealing a conflict here leaks nothing; it covers a race with another account.
        if (await accountRepository.IsVerifiedEmailInUseAsync(account.Email, account.Id, cancellationToken))
        {
            throw new EmailAlreadyInUseException();
        }

        account.MarkEmailVerified();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<PatientAccount> GetCurrentAccountAsync(CancellationToken cancellationToken)
    {
        var accountId = currentUser.GetRequiredAccountId(AccountType.Patient);
        return await accountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new ForbiddenException();
    }
}
