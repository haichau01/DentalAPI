using DentalAPI.Application.Common.Pagination;
using DentalAPI.Domain.Patients;

namespace DentalAPI.Application.Patients;

/// <summary>Loads and stores patients. Soft-deleted patients are never returned.</summary>
public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Returns patients ordered by full name.</summary>
    Task<PagedResult<Patient>> ListAsync(PageQuery query, CancellationToken cancellationToken);

    void Add(Patient patient);

    /// <summary>Marks the patient for deletion; the persistence layer turns this into a soft delete.</summary>
    void Remove(Patient patient);
}
