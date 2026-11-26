using DentalAPI.Application.Common.Pagination;

namespace DentalAPI.Application.Patients;

/// <summary>Use cases for managing patients.</summary>
public interface IPatientService
{
    Task<PagedResult<PatientDto>> ListAsync(PageQuery query, CancellationToken cancellationToken);

    /// <exception cref="Common.Exceptions.NotFoundException">The patient does not exist.</exception>
    Task<PatientDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <exception cref="FluentValidation.ValidationException">The request is invalid.</exception>
    Task<PatientDto> CreateAsync(SavePatientRequest request, CancellationToken cancellationToken);

    /// <exception cref="FluentValidation.ValidationException">The request is invalid.</exception>
    /// <exception cref="Common.Exceptions.NotFoundException">The patient does not exist.</exception>
    Task<PatientDto> UpdateAsync(Guid id, SavePatientRequest request, CancellationToken cancellationToken);

    /// <summary>Soft-deletes the patient so treatment history and invoices stay intact.</summary>
    /// <exception cref="Common.Exceptions.NotFoundException">The patient does not exist.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
