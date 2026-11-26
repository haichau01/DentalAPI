using DentalAPI.Application.Common.Pagination;
using DentalAPI.Application.PatientAccounts;
using DentalAPI.Application.StaffAccounts;
using DentalAPI.Domain.Accounts;
using DentalAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalAPI.Infrastructure.Repositories;

public sealed class StaffAccountRepository(DentalDbContext dbContext) : IStaffAccountRepository
{
    public Task<StaffAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.StaffAccounts.FirstOrDefaultAsync(account => account.Id == id, cancellationToken);

    public Task<StaffAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        dbContext.StaffAccounts.FirstOrDefaultAsync(account => account.Email == email, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        dbContext.StaffAccounts.AnyAsync(account => account.Email == email, cancellationToken);

    public Task<bool> AnyAdminAsync(CancellationToken cancellationToken) =>
        dbContext.StaffAccounts.AnyAsync(account => account.Role == StaffRole.Admin, cancellationToken);

    public async Task<PagedResult<StaffAccount>> ListAsync(PageQuery query, CancellationToken cancellationToken)
    {
        var accounts = dbContext.StaffAccounts.AsNoTracking();

        var totalCount = await accounts.CountAsync(cancellationToken);
        var items = await accounts
            .OrderBy(account => account.FullName)
            .ThenBy(account => account.Id)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<StaffAccount>(items, query.Page, query.PageSize, totalCount);
    }

    public void Add(StaffAccount account) => dbContext.StaffAccounts.Add(account);

    public void Remove(StaffAccount account) => dbContext.StaffAccounts.Remove(account);
}

public sealed class PatientAccountRepository(DentalDbContext dbContext) : IPatientAccountRepository
{
    public Task<PatientAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.PatientAccounts.FirstOrDefaultAsync(account => account.Id == id, cancellationToken);

    public Task<PatientAccount?> GetActiveByPhoneAsync(string phoneNumber, CancellationToken cancellationToken) =>
        dbContext.PatientAccounts.FirstOrDefaultAsync(
            account => account.PhoneNumber == phoneNumber && account.Status == PatientAccountStatus.Active,
            cancellationToken);

    public Task<PatientAccount?> GetPendingByPhoneAsync(string phoneNumber, CancellationToken cancellationToken) =>
        dbContext.PatientAccounts
            .Where(account => account.PhoneNumber == phoneNumber && account.Status == PatientAccountStatus.PendingVerification)
            .OrderByDescending(account => account.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PatientAccount?> GetActiveByVerifiedEmailAsync(string email, CancellationToken cancellationToken) =>
        dbContext.PatientAccounts.FirstOrDefaultAsync(
            account => account.Email == email && account.IsEmailVerified && account.Status == PatientAccountStatus.Active,
            cancellationToken);

    public Task<bool> IsVerifiedEmailInUseAsync(string email, Guid excludeAccountId, CancellationToken cancellationToken) =>
        dbContext.PatientAccounts.AnyAsync(
            account => account.Email == email && account.IsEmailVerified && account.Id != excludeAccountId,
            cancellationToken);

    // ExecuteDelete bypasses the soft-delete interceptor on purpose; see the interface for why this data is not kept.
    // IgnoreQueryFilters so pending rows that were soft-deleted earlier are purged as well.
    public Task<int> DeletePendingNotUpdatedSinceAsync(DateTimeOffset cutoff, CancellationToken cancellationToken) =>
        dbContext.PatientAccounts
            .IgnoreQueryFilters()
            .Where(account => account.Status == PatientAccountStatus.PendingVerification && account.UpdatedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

    public void Add(PatientAccount account) => dbContext.PatientAccounts.Add(account);

    public void Remove(PatientAccount account) => dbContext.PatientAccounts.Remove(account);
}
