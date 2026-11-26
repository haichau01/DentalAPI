using DentalAPI.Application.Common.Addresses;
using DentalAPI.Application.Common.Time;
using DentalAPI.Application.Common.Validation;
using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Patients;
using FluentValidation;

namespace DentalAPI.Application.PatientAccounts;

public sealed record RegisterPatientAccountRequest(
    string FullName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string? Email,
    string Password,
    AddressDto? Address = null);

/// <summary>Returned when registration is accepted and a code has been sent to the phone.</summary>
public sealed record PatientRegistrationDto(DateTimeOffset VerificationCodeExpiresAt);

public sealed record VerifyPhoneRequest(string PhoneNumber, string Code);

public sealed record ResendVerificationCodeRequest(string PhoneNumber);

/// <summary>Sign-in with either the phone number or a verified email as <see cref="Identifier"/>.</summary>
public sealed record PatientLoginRequest(string Identifier, string Password);

public sealed record SendEmailVerificationCodeRequest(string Email);

public sealed record VerifyEmailRequest(string Code);

public sealed class RegisterPatientAccountRequestValidator : AbstractValidator<RegisterPatientAccountRequest>
{
    public RegisterPatientAccountRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(request => request.FullName).NotEmpty().MaximumLength(Patient.FullNameMaxLength);

        RuleFor(request => request.DateOfBirth)
            .Must(dateOfBirth => dateOfBirth <= ClinicClock.Today(timeProvider))
            .WithMessage("Date of birth cannot be in the future.");

        RuleFor(request => request.PhoneNumber).ValidPhoneNumber(PatientAccount.PhoneNumberMaxLength);

        RuleFor(request => request.Email)
            .ValidEmail(PatientAccount.EmailMaxLength)
            .When(request => !string.IsNullOrWhiteSpace(request.Email));

        RuleFor(request => request.Password).ValidPassword();

        RuleFor(request => request.Address!)
            .SetValidator(new AddressValidator())
            .When(request => request.Address is not null);
    }
}

public sealed class VerifyPhoneRequestValidator : AbstractValidator<VerifyPhoneRequest>
{
    public VerifyPhoneRequestValidator()
    {
        RuleFor(request => request.PhoneNumber).ValidPhoneNumber(PatientAccount.PhoneNumberMaxLength);
        RuleFor(request => request.Code).ValidVerificationCode();
    }
}

public sealed class ResendVerificationCodeRequestValidator : AbstractValidator<ResendVerificationCodeRequest>
{
    public ResendVerificationCodeRequestValidator()
    {
        RuleFor(request => request.PhoneNumber).ValidPhoneNumber(PatientAccount.PhoneNumberMaxLength);
    }
}

public sealed class PatientLoginRequestValidator : AbstractValidator<PatientLoginRequest>
{
    public PatientLoginRequestValidator()
    {
        RuleFor(request => request.Identifier).NotEmpty().MaximumLength(PatientAccount.EmailMaxLength);
        RuleFor(request => request.Password).NotEmpty().MaximumLength(ContactRules.PasswordMaxLength);
    }
}

public sealed class SendEmailVerificationCodeRequestValidator : AbstractValidator<SendEmailVerificationCodeRequest>
{
    public SendEmailVerificationCodeRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().ValidEmail(PatientAccount.EmailMaxLength);
    }
}

public sealed class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(request => request.Code).ValidVerificationCode();
    }
}

internal static class VerificationCodeRules
{
    private static readonly string _codePattern = $"^[0-9]{{{VerificationCode.CodeLength}}}$";

    public static IRuleBuilderOptions<T, string> ValidVerificationCode<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().Matches(_codePattern);
}
