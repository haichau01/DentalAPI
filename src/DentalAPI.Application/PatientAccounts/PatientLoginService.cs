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

        var identifier = NormalizeIdentifier(request.Identifier);
        var account = await FindActiveAccountAsync(identifier, cancellationToken);
        await credentialChecker.EnsureValidAsync(identifier, account, request.Password, cancellationToken);

        var issued = tokenPairIssuer.Issue(AccessTokenSubject.ForPatient(account!));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return issued.Tokens;
    }

    private static bool IsEmail(string identifier) => identifier.Contains(EmailMarker);

    private static string NormalizeIdentifier(string identifier) =>
        IsEmail(identifier) ? PatientAccount.NormalizeEmail(identifier)! : PhoneNumber.Normalize(identifier);

    private Task<PatientAccount?> FindActiveAccountAsync(string identifier, CancellationToken cancellationToken) =>
        IsEmail(identifier)
            ? accountRepository.GetActiveByVerifiedEmailAsync(identifier, cancellationToken)
            : accountRepository.GetActiveByPhoneAsync(identifier, cancellationToken);
}
