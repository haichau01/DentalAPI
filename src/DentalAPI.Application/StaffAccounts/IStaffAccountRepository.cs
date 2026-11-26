using DentalAPI.Application.Common.Pagination;
using DentalAPI.Domain.Accounts;

namespace DentalAPI.Application.StaffAccounts;

/// <summary>Loads and stores staff accounts. Emails passed in must already be normalized.</summary>
public interface IStaffAccountRepository
{
    Task<StaffAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<StaffAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task<bool> AnyAdminAsync(CancellationToken cancellationToken);

    /// <summary>Returns staff ordered by full name.</summary>
    Task<PagedResult<StaffAccount>> ListAsync(PageQuery query, CancellationToken cancellationToken);

    void Add(StaffAccount account);

    /// <summary>Marks the account for deletion; the persistence layer turns this into a soft delete.</summary>
    void Remove(StaffAccount account);
}
