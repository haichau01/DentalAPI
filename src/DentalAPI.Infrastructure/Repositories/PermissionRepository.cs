using DentalAPI.Application.Authorization;
using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Authorization;
using DentalAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalAPI.Infrastructure.Repositories;

public sealed class PermissionRepository(DentalDbContext dbContext) : IPermissionRepository
{
    public async Task<IReadOnlyList<StaffRolePermission>> ListForRoleAsync(StaffRole role, CancellationToken cancellationToken) =>
        await dbContext.StaffRolePermissions.Where(item => item.Role == role).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StaffPermissionOverride>> ListOverridesAsync(Guid staffAccountId, CancellationToken cancellationToken) =>
        await dbContext.StaffPermissionOverrides.Where(item => item.StaffAccountId == staffAccountId).ToListAsync(cancellationToken);

    public void Add(StaffRolePermission rolePermission) => dbContext.StaffRolePermissions.Add(rolePermission);

    public void Remove(StaffRolePermission rolePermission) => dbContext.StaffRolePermissions.Remove(rolePermission);

    public void Add(StaffPermissionOverride permissionOverride) => dbContext.StaffPermissionOverrides.Add(permissionOverride);

    public void Remove(StaffPermissionOverride permissionOverride) => dbContext.StaffPermissionOverrides.Remove(permissionOverride);
}
