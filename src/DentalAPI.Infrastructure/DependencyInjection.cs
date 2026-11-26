using DentalAPI.Application.Common.Persistence;
using DentalAPI.Application.Patients;
using DentalAPI.Infrastructure.Persistence;
using DentalAPI.Infrastructure.Persistence.Interceptors;
using DentalAPI.Infrastructure.Persistence.Seeding;
using DentalAPI.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DentalAPI.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "Default";

    /// <summary>Registers EF Core, repositories and the database health check.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<AuditAndSoftDeleteInterceptor>();

        // The connection string is read when the DbContext is created, not at startup,
        // so integration tests can supply their own through configuration.
        services.AddDbContext<DentalDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string '{ConnectionStringName}' is not configured. " +
                    "Set it with 'dotnet user-secrets' or the ConnectionStrings__Default environment variable.");
            }

            options.UseSqlServer(connectionString);
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditAndSoftDeleteInterceptor>());
        });

        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<DentalDbContext>());
        services.AddScoped<IPatientRepository, PatientRepository>();

        services.AddHealthChecks().AddDbContextCheck<DentalDbContext>("database");

        return services;
    }

    /// <summary>Applies pending migrations and seeds fake data. Development only: production migrations run as a deploy step.</summary>
    public static async Task InitializeDevelopmentDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DentalDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
        await DevelopmentDataSeeder.SeedAsync(dbContext, cancellationToken);
    }
}
