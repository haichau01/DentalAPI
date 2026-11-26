using DentalAPI.Application.Auth;
using DentalAPI.Application.Authorization;
using DentalAPI.Application.Common.Persistence;
using DentalAPI.Application.Common.Security;
using DentalAPI.Application.PatientAccounts;
using DentalAPI.Application.Patients;
using DentalAPI.Application.StaffAccounts;
using DentalAPI.Infrastructure.Authorization;
using DentalAPI.Infrastructure.Persistence;
using DentalAPI.Infrastructure.Persistence.Interceptors;
using DentalAPI.Infrastructure.Persistence.Seeding;
using DentalAPI.Infrastructure.Repositories;
using DentalAPI.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DentalAPI.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "Default";

    /// <summary>Registers EF Core, repositories, token and password services, and the database health check.</summary>
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
        services.AddScoped<IStaffAccountRepository, StaffAccountRepository>();
        services.AddScoped<IPatientAccountRepository, PatientAccountRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IVerificationCodeRepository, VerificationCodeRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();

        services.AddSecurity();

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

    private static void AddSecurity(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .Validate(
                options => options.HasValidSigningKey(),
                $"Jwt:SigningKey must be at least {JwtOptions.MinSigningKeyBytes} bytes. Set it with user-secrets or the Jwt__SigningKey environment variable.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Issuer) && !string.IsNullOrWhiteSpace(options.Audience),
                "Jwt:Issuer and Jwt:Audience must be configured.")
            .ValidateOnStart();

        services.AddOptions<BootstrapAdminOptions>()
            .BindConfiguration(BootstrapAdminOptions.SectionName)
            .Validate(
                options => !options.IsConfigured || options.Password.Length >= BootstrapAdminOptions.MinPasswordLength,
                $"BootstrapAdmin:Password must be at least {BootstrapAdminOptions.MinPasswordLength} characters.")
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddSingleton<IVerificationCodeGenerator, VerificationCodeGenerator>();
        services.AddScoped<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddScoped<IRefreshTokenFactory, RefreshTokenFactory>();

        services.AddMemoryCache();
        services.AddSingleton<PermissionCache>();
        services.AddScoped<IEffectivePermissionProvider, CachedEffectivePermissionProvider>();
    }
}
