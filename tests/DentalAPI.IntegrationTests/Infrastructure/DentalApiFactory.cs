using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MsSql;

namespace DentalAPI.IntegrationTests.Infrastructure;

/// <summary>
/// Starts the API against a real SQL Server in a container (requires Docker).
/// The Development environment applies migrations and seeds fake data on startup.
/// </summary>
public sealed class DentalApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@example.com";
    public const string AdminPassword = "Admin-Test-Passw0rd";

    // All test requests share one client address, so the per-IP limit is raised; rate limiting is checked manually.
    private const string HighRateLimit = "100000";
    private const int SigningKeyBytes = 48;

    private readonly MsSqlContainer _database = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async Task InitializeAsync() => await _database.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", _database.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(SigningKeyBytes)));
        builder.UseSetting("BootstrapAdmin:Email", AdminEmail);
        builder.UseSetting("BootstrapAdmin:Password", AdminPassword);
        builder.UseSetting("RateLimiting:PublicAuth:PermitLimit", HighRateLimit);
    }
}
