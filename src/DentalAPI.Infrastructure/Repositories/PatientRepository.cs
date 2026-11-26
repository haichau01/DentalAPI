using DentalAPI.Application.Common.Pagination;
using DentalAPI.Application.Patients;
using DentalAPI.Domain.Patients;
using DentalAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalAPI.Infrastructure.Repositories;

public sealed class PatientRepository(DentalDbContext dbContext) : IPatientRepository
{
    public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Patients.FirstOrDefaultAsync(patient => patient.Id == id, cancellationToken);

    public async Task<PagedResult<Patient>> ListAsync(PageQuery query, CancellationToken cancellationToken)
    {
        var patients = dbContext.Patients.AsNoTracking();

        var totalCount = await patients.CountAsync(cancellationToken);
        var items = await patients
            .OrderBy(patient => patient.FullName)
            .ThenBy(patient => patient.Id)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Patient>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<Guid>> ListUnlinkedIdsByPhoneAsync(string phoneNumber, CancellationToken cancellationToken) =>
        await dbContext.Patients
            .Where(patient => patient.PhoneNumber == phoneNumber)
            .Where(patient => !dbContext.PatientAccounts.Any(account => account.PatientId == patient.Id))
            .Select(patient => patient.Id)
            .ToListAsync(cancellationToken);

    public void Add(Patient patient) => dbContext.Patients.Add(patient);

    public void Remove(Patient patient) => dbContext.Patients.Remove(patient);
}
