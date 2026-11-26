using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DentalAPI.Infrastructure.Persistence;

/// <summary>
/// Used only by <c>dotnet ef</c> to create migrations. Creating a migration does not connect
/// to the database, so the connection string here is a placeholder without credentials.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DentalDbContext>
{
    public DentalDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DentalDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=DentalDb;Integrated Security=true;TrustServerCertificate=True")
            .Options;

        return new DentalDbContext(options);
    }
}
