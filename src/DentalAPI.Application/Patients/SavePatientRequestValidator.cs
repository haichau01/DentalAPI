using DentalAPI.Application.Common.Time;
using DentalAPI.Domain.Patients;
using FluentValidation;

namespace DentalAPI.Application.Patients;

public sealed class SavePatientRequestValidator : AbstractValidator<SavePatientRequest>
{
    // Digits with an optional leading "+", e.g. 0901234567 or +84901234567.
    private const string PhoneNumberPattern = @"^\+?[0-9]{9,15}$";

    public SavePatientRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(request => request.FullName)
            .NotEmpty()
            .MaximumLength(Patient.FullNameMaxLength);

        RuleFor(request => request.DateOfBirth)
            .Must(dateOfBirth => dateOfBirth <= ClinicClock.Today(timeProvider))
            .WithMessage("Date of birth cannot be in the future.");

        RuleFor(request => request.PhoneNumber)
            .NotEmpty()
            .MaximumLength(Patient.PhoneNumberMaxLength)
            .Matches(PhoneNumberPattern);

        RuleFor(request => request.Email)
            .EmailAddress()
            .MaximumLength(Patient.EmailMaxLength)
            .When(request => !string.IsNullOrWhiteSpace(request.Email));

        RuleFor(request => request.MedicalNotes)
            .MaximumLength(Patient.MedicalNotesMaxLength);
    }
}
