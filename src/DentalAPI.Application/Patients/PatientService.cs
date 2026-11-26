using DentalAPI.Application.Common.Exceptions;
using DentalAPI.Application.Common.Pagination;
using DentalAPI.Application.Common.Persistence;
using DentalAPI.Domain.Patients;
using FluentValidation;

namespace DentalAPI.Application.Patients;

public sealed class PatientService(
    IPatientRepository patientRepository,
    IUnitOfWork unitOfWork,
    IValidator<SavePatientRequest> requestValidator,
    IValidator<PageQuery> pageQueryValidator) : IPatientService
{
    public async Task<PagedResult<PatientDto>> ListAsync(PageQuery query, CancellationToken cancellationToken)
    {
        await pageQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

        var patients = await patientRepository.ListAsync(query, cancellationToken);
        return patients.Map(PatientDto.FromEntity);
    }

    public async Task<PatientDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var patient = await GetExistingAsync(id, cancellationToken);
        return PatientDto.FromEntity(patient);
    }

    public async Task<PatientDto> CreateAsync(SavePatientRequest request, CancellationToken cancellationToken)
    {
        await requestValidator.ValidateAndThrowAsync(request, cancellationToken);

        var patient = new Patient(request.ToDetails());
        patientRepository.Add(patient);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PatientDto.FromEntity(patient);
    }

    public async Task<PatientDto> UpdateAsync(Guid id, SavePatientRequest request, CancellationToken cancellationToken)
    {
        await requestValidator.ValidateAndThrowAsync(request, cancellationToken);

        var patient = await GetExistingAsync(id, cancellationToken);
        patient.UpdateDetails(request.ToDetails());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PatientDto.FromEntity(patient);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var patient = await GetExistingAsync(id, cancellationToken);
        patientRepository.Remove(patient);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Patient> GetExistingAsync(Guid id, CancellationToken cancellationToken) =>
        await patientRepository.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException(nameof(Patient), id);
}
