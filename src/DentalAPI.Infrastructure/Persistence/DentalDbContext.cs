using DentalAPI.Application.Common.Persistence;
using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Authorization;
using DentalAPI.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace DentalAPI.Infrastructure.Persistence;

public sealed class DentalDbContext(DbContextOptions<DentalDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<StaffAccount> StaffAccounts => Set<StaffAccount>();

    public DbSet<PatientAccount> PatientAccounts => Set<PatientAccount>();

    public DbSet<VerificationCode> VerificationCodes => Set<VerificationCode>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<StaffRolePermission> StaffRolePermissions => Set<StaffRolePermission>();

    public DbSet<StaffPermissionOverride> StaffPermissionOverrides => Set<StaffPermissionOverride>();

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DentalDbContext).Assembly);
    }
}
