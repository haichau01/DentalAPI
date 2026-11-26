using DentalAPI.Application.Common.Pagination;
using DentalAPI.Application.Common.Persistence;
using DentalAPI.Application.Patients;
using DentalAPI.Domain.Patients;

namespace DentalAPI.UnitTests.TestDoubles;

/// <summary>Repository and unit of work backed by a list, for testing service logic without a database.</summary>
public sealed class InMemoryPatientRepository : IPatientRepository, IUnitOfWork
{
    private readonly List<Patient> _patients = [];

    public IReadOnlyList<Patient> Patients => _patients;

    /// <summary>Patients that already have an account; the database works this out from the PatientAccounts table.</summary>
    public HashSet<Guid> LinkedPatientIds { get; } = [];

    public int SaveChangesCallCount { get; private set; }

    public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_patients.SingleOrDefault(patient => patient.Id == id));

    public Task<PagedResult<Patient>> ListAsync(PageQuery query, CancellationToken cancellationToken)
    {
        var items = _patients.OrderBy(patient => patient.FullName).Skip(query.Skip).Take(query.PageSize).ToList();
        return Task.FromResult(new PagedResult<Patient>(items, query.Page, query.PageSize, _patients.Count));
    }

    public Task<IReadOnlyList<Guid>> ListUnlinkedIdsByPhoneAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> ids = _patients
            .Where(patient => patient.PhoneNumber == phoneNumber && !LinkedPatientIds.Contains(patient.Id))
            .Select(patient => patient.Id)
            .ToList();
        return Task.FromResult(ids);
    }

    public void Add(Patient patient) => _patients.Add(patient);

    public void Remove(Patient patient) => _patients.Remove(patient);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}
