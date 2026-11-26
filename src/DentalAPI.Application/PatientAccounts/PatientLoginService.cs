using DentalAPI.Application.Auth;
using DentalAPI.Application.Common.Exceptions;
using DentalAPI.Application.Common.Persistence;
using DentalAPI.Application.Common.Security;
using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Common;
using FluentValidation;

namespace DentalAPI.Application.PatientAccounts;

/// <summary>Patient sign-in with a phone number or a verified email.</summary>
public interface IPatientLoginService
{
    /// <exception cref="AuthenticationFailedException">Unknown, unverified or locked account, or wrong password.</exception>
    Task<AuthTokensDto> LoginAsync(PatientLoginRequest request, CancellationToken cancellationToken);
}

public sealed class PatientLoginService(
    IPatientAccountRepository accountRepository,
    PasswordCredentialChecker credentialChecker,
    TokenPairIssuer tokenPairIssuer,
    IUnitOfWork unitOfWork,
    IValidator<PatientLoginRequest> requestValidator) : IPatientLoginService
{
    private const char EmailMarker = '@';

    public async Task<AuthTokensDto> LoginAsync(PatientLoginRequest request, CancellationToken cancellationToken)
    {
        await requestValidator.ValidateAndThrowAsync(request, cancellationToken);

        var account = await FindActiveAccountAsync(request.Identifier, cancellationToken);
        await credentialChecker.EnsureValidAsync(account, request.Password, cancellationToken);

        var issued = tokenPairIssuer.Issue(AccessTokenSubject.ForPatient(account!));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return issued.Tokens;
    }

    private Task<PatientAccount?> FindActiveAccountAsync(string identifier, CancellationToken cancellationToken) =>
        identifier.Contains(EmailMarker)
            ? accountRepository.GetActiveByVerifiedEmailAsync(PatientAccount.NormalizeEmail(identifier)!, cancellationToken)
            : accountRepository.GetActiveByPhoneAsync(PhoneNumber.Normalize(identifier), cancellationToken);
}
