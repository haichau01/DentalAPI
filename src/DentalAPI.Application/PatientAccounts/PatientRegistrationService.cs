using DentalAPI.Application.Auth;
using DentalAPI.Application.Common.Exceptions;
using DentalAPI.Application.Common.Persistence;
using DentalAPI.Application.Common.Security;
using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Common;
using FluentValidation;

namespace DentalAPI.Application.PatientAccounts;

/// <summary>Self-registration of patients, confirmed by a code sent to their phone.</summary>
public interface IPatientRegistrationService
{
    /// <summary>Creates a pending account and sends a code by SMS. Re-registering a pending phone replaces the old attempt.</summary>
    /// <exception cref="ValidationException">The request is invalid.</exception>
    /// <exception cref="PhoneNumberAlreadyRegisteredException">An active account already uses the phone number.</exception>
    Task<PatientRegistrationDto> RegisterAsync(RegisterPatientAccountRequest request, CancellationToken cancellationToken);

    /// <summary>Activates the account, links it to a patient record and signs the patient in.</summary>
    /// <exception cref="InvalidVerificationCodeException">No pending registration or the code is invalid.</exception>
    Task<AuthTokensDto> VerifyPhoneAsync(VerifyPhoneRequest request, CancellationToken cancellationToken);

    /// <summary>Sends a new code to a pending registration. Does nothing for unknown phones so the result reveals nothing.</summary>
    Task ResendCodeAsync(ResendVerificationCodeRequest request, CancellationToken cancellationToken);
}

public sealed class PatientRegistrationService(
    IPatientAccountRepository accountRepository,
    IPasswordHasher passwordHasher,
    VerificationCodeService verificationCodeService,
    PatientAccountLinker accountLinker,
    TokenPairIssuer tokenPairIssuer,
    IUnitOfWork unitOfWork,
    IValidator<RegisterPatientAccountRequest> registerValidator,
    IValidator<VerifyPhoneRequest> verifyValidator,
    IValidator<ResendVerificationCodeRequest> resendValidator) : IPatientRegistrationService
{
    public async Task<PatientRegistrationDto> RegisterAsync(RegisterPatientAccountRequest request, CancellationToken cancellationToken)
    {
        await registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var phoneNumber = PhoneNumber.Normalize(request.PhoneNumber);
        if (await accountRepository.GetActiveByPhoneAsync(phoneNumber, cancellationToken) is not null)
        {
            throw new PhoneNumberAlreadyRegisteredException();
        }

        var profile = new PendingPatientProfile(request.FullName, request.DateOfBirth, request.Address?.ToDomain());
        var passwordHash = passwordHasher.Hash(request.Password);

        // Reusing the pending row instead of soft-deleting it keeps no stale copy of the earlier details.
        var previousAttempt = await accountRepository.GetPendingByPhoneAsync(phoneNumber, cancellationToken);
        if (previousAttempt is not null)
        {
            previousAttempt.RestartRegistration(request.Email, passwordHash, profile);
        }
        else
        {
            accountRepository.Add(new PatientAccount(phoneNumber, request.Email, passwordHash, profile));
        }

        var codeExpiresAt = await verificationCodeService.SendAsync(VerificationChannel.Sms, phoneNumber, cancellationToken);
        return new PatientRegistrationDto(codeExpiresAt);
    }

    public async Task<AuthTokensDto> VerifyPhoneAsync(VerifyPhoneRequest request, CancellationToken cancellationToken)
    {
        await verifyValidator.ValidateAndThrowAsync(request, cancellationToken);

        var phoneNumber = PhoneNumber.Normalize(request.PhoneNumber);
        var account = await accountRepository.GetPendingByPhoneAsync(phoneNumber, cancellationToken)
            ?? throw new InvalidVerificationCodeException();

        await verificationCodeService.VerifyAsync(VerificationChannel.Sms, phoneNumber, request.Code, cancellationToken);

        // Another registration for the same phone may have been verified since this one started.
        if (await accountRepository.GetActiveByPhoneAsync(phoneNumber, cancellationToken) is not null)
        {
            throw new PhoneNumberAlreadyRegisteredException();
        }

        var patientId = await accountLinker.FindOrCreatePatientAsync(account, cancellationToken);
        account.Activate(patientId);
        var issued = tokenPairIssuer.Issue(AccessTokenSubject.ForPatient(account));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return issued.Tokens;
    }

    public async Task ResendCodeAsync(ResendVerificationCodeRequest request, CancellationToken cancellationToken)
    {
        await resendValidator.ValidateAndThrowAsync(request, cancellationToken);

        var phoneNumber = PhoneNumber.Normalize(request.PhoneNumber);
        if (await accountRepository.GetPendingByPhoneAsync(phoneNumber, cancellationToken) is null)
        {
            return;
        }

        await verificationCodeService.SendAsync(VerificationChannel.Sms, phoneNumber, cancellationToken);
    }
}
