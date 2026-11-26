using DentalAPI.Application.Common.Addresses;
using DentalAPI.Application.Common.Time;
using DentalAPI.Application.Common.Validation;
using DentalAPI.Domain.Patients;
using FluentValidation;

namespace DentalAPI.Application.Patients;

public sealed class SavePatientRequestValidator : AbstractValidator<SavePatientRequest>
{
    public SavePatientRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(request => request.FullName)
            .NotEmpty()
            .MaximumLength(Patient.FullNameMaxLength);

        RuleFor(request => request.DateOfBirth)
            .Must(dateOfBirth => dateOfBirth <= ClinicClock.Today(timeProvider))
            .WithMessage("Date of birth cannot be in the future.");

        RuleFor(request => request.PhoneNumber).ValidPhoneNumber(Patient.PhoneNumberMaxLength);

        RuleFor(request => request.Email)
            .ValidEmail(Patient.EmailMaxLength)
            .When(request => !string.IsNullOrWhiteSpace(request.Email));

        RuleFor(request => request.MedicalNotes)
            .MaximumLength(Patient.MedicalNotesMaxLength);

        RuleFor(request => request.Address!)
            .SetValidator(new AddressValidator())
            .When(request => request.Address is not null);
    }
}
