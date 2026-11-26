using DentalAPI.Application.Common.Addresses;
using DentalAPI.Application.Common.Exceptions;
using DentalAPI.Application.Common.Persistence;
using DentalAPI.Application.Common.Security;
using DentalAPI.Domain.Patients;
using FluentValidation;

namespace DentalAPI.Application.Patients;

/// <summary>Request body for a patient changing their own address; null clears it.</summary>
public sealed record UpdateAddressRequest(AddressDto? Address);

public sealed class UpdateAddressRequestValidator : AbstractValidator<UpdateAddressRequest>
{
    public UpdateAddressRequestValidator()
    {
        RuleFor(request => request.Address!)
            .SetValidator(new AddressValidator())
            .When(request => request.Address is not null);
    }
}

/// <summary>Use cases for a signed-in patient on their own record. The record id always comes from the access token.</summary>
public interface ICurrentPatientService
{
    /// <exception cref="ForbiddenException">The caller is not a linked patient.</exception>
    Task<PatientProfileDto> GetProfileAsync(CancellationToken cancellationToken);

    /// <exception cref="ValidationException">The address is invalid.</exception>
    Task<PatientProfileDto> UpdateAddressAsync(UpdateAddressRequest request, CancellationToken cancellationToken);
}

public sealed class CurrentPatientService(
    ICurrentUser currentUser,
    IPatientRepository patientRepository,
    IUnitOfWork unitOfWork,
    IValidator<UpdateAddressRequest> requestValidator) : ICurrentPatientService
{
    public async Task<PatientProfileDto> GetProfileAsync(CancellationToken cancellationToken)
    {
        var patient = await GetCurrentPatientAsync(cancellationToken);
        return PatientProfileDto.FromEntity(patient);
    }

    public async Task<PatientProfileDto> UpdateAddressAsync(UpdateAddressRequest request, CancellationToken cancellationToken)
    {
        await requestValidator.ValidateAndThrowAsync(request, cancellationToken);

        var patient = await GetCurrentPatientAsync(cancellationToken);
        patient.ChangeAddress(request.Address?.ToDomain());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PatientProfileDto.FromEntity(patient);
    }

    private async Task<Patient> GetCurrentPatientAsync(CancellationToken cancellationToken)
    {
        var patientId = currentUser.GetRequiredPatientId();
        return await patientRepository.GetByIdAsync(patientId, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), patientId);
    }
}
