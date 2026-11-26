using DentalAPI.Application.Common.Persistence;
using DentalAPI.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace DentalAPI.Infrastructure.Persistence;

public sealed class DentalDbContext(DbContextOptions<DentalDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Patient> Patients => Set<Patient>();

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DentalDbContext).Assembly);
    }
}
