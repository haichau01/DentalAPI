using DentalAPI.Application.Auth;
using DentalAPI.Application.Authorization;
using DentalAPI.Application.Common.Pagination;
using DentalAPI.Application.PatientAccounts;
using DentalAPI.Application.StaffAccounts;
using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Authorization;

namespace DentalAPI.UnitTests.TestDoubles;

public sealed class InMemoryPatientAccountRepository : IPatientAccountRepository
{
    public List<PatientAccount> Accounts { get; } = [];

    public Task<PatientAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.SingleOrDefault(account => account.Id == id));

    public Task<PatientAccount?> GetActiveByPhoneAsync(string phoneNumber, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.SingleOrDefault(account => account.PhoneNumber == phoneNumber && account.IsActive));

    public Task<PatientAccount?> GetPendingByPhoneAsync(string phoneNumber, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.LastOrDefault(account => account.PhoneNumber == phoneNumber && !account.IsActive));

    public Task<PatientAccount?> GetActiveByVerifiedEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.SingleOrDefault(account => account.Email == email && account.IsEmailVerified && account.IsActive));

    public Task<bool> IsVerifiedEmailInUseAsync(string email, Guid excludeAccountId, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.Any(account => account.Email == email && account.IsEmailVerified && account.Id != excludeAccountId));

    public void Add(PatientAccount account) => Accounts.Add(account);

    public void Remove(PatientAccount account) => Accounts.Remove(account);
}

public sealed class InMemoryStaffAccountRepository : IStaffAccountRepository
{
    public List<StaffAccount> Accounts { get; } = [];

    public Task<StaffAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.SingleOrDefault(account => account.Id == id));

    public Task<StaffAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.SingleOrDefault(account => account.Email == email));

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.Any(account => account.Email == email));

    public Task<bool> AnyAdminAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.Any(account => account.IsAdmin));

    public Task<PagedResult<StaffAccount>> ListAsync(PageQuery query, CancellationToken cancellationToken)
    {
        var items = Accounts.OrderBy(account => account.FullName).Skip(query.Skip).Take(query.PageSize).ToList();
        return Task.FromResult(new PagedResult<StaffAccount>(items, query.Page, query.PageSize, Accounts.Count));
    }

    public void Add(StaffAccount account) => Accounts.Add(account);

    public void Remove(StaffAccount account) => Accounts.Remove(account);
}

public sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = [];

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Tokens.SingleOrDefault(token => token.TokenHash == tokenHash));

    public Task<IReadOnlyList<RefreshToken>> ListUnrevokedByAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        IReadOnlyList<RefreshToken> tokens = Tokens.Where(token => token.AccountId == accountId && !token.IsRevoked).ToList();
        return Task.FromResult(tokens);
    }

    public void Add(RefreshToken refreshToken) => Tokens.Add(refreshToken);
}

public sealed class InMemoryVerificationCodeRepository : IVerificationCodeRepository
{
    public List<VerificationCode> Codes { get; } = [];

    public Task<VerificationCode?> GetLatestAsync(VerificationChannel channel, string destination, CancellationToken cancellationToken) =>
        Task.FromResult(Codes.LastOrDefault(code => code.Channel == channel && code.Destination == destination));

    public void Add(VerificationCode verificationCode) => Codes.Add(verificationCode);
}

public sealed class InMemoryPermissionRepository : IPermissionRepository
{
    public List<StaffRolePermission> RolePermissions { get; } = [.. DefaultStaffRolePermissions.All];

    public List<StaffPermissionOverride> Overrides { get; } = [];

    public Task<IReadOnlyList<StaffRolePermission>> ListForRoleAsync(StaffRole role, CancellationToken cancellationToken)
    {
        IReadOnlyList<StaffRolePermission> items = RolePermissions.Where(item => item.Role == role).ToList();
        return Task.FromResult(items);
    }

    public Task<IReadOnlyList<StaffPermissionOverride>> ListOverridesAsync(Guid staffAccountId, CancellationToken cancellationToken)
    {
        IReadOnlyList<StaffPermissionOverride> items = Overrides.Where(item => item.StaffAccountId == staffAccountId).ToList();
        return Task.FromResult(items);
    }

    public void Add(StaffRolePermission rolePermission) => RolePermissions.Add(rolePermission);

    public void Remove(StaffRolePermission rolePermission) => RolePermissions.Remove(rolePermission);

    public void Add(StaffPermissionOverride permissionOverride) => Overrides.Add(permissionOverride);

    public void Remove(StaffPermissionOverride permissionOverride) => Overrides.Remove(permissionOverride);
}
