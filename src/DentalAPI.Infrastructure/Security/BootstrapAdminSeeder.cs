using DentalAPI.Application.Common.Persistence;
using DentalAPI.Application.Common.Security;
using DentalAPI.Application.StaffAccounts;
using DentalAPI.Domain.Accounts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DentalAPI.Infrastructure.Security;

/// <summary>
/// Settings from "BootstrapAdmin". Admins cannot be created through the API, so this is the only way to get
/// the first one. Set the password with user-secrets or an environment variable.
/// </summary>
public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";
    public const int MinPasswordLength = 12;
    private const string DefaultFullName = "Quản trị viên";

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FullName { get; set; } = DefaultFullName;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
}

/// <summary>Creates the first admin on startup when no admin exists yet. Runs in every environment.</summary>
public static class BootstrapAdminSeeder
{
    public static async Task EnsureBootstrapAdminAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var options = provider.GetRequiredService<IOptions<BootstrapAdminOptions>>().Value;
        var logger = provider.GetRequiredService<ILogger<BootstrapAdminOptions>>();
        var repository = provider.GetRequiredService<IStaffAccountRepository>();

        if (await repository.AnyAdminAsync(cancellationToken))
        {
            return;
        }

        if (!options.IsConfigured)
        {
            logger.LogWarning("No admin account exists and BootstrapAdmin is not configured; nobody can manage staff accounts.");
            return;
        }

        var passwordHash = provider.GetRequiredService<IPasswordHasher>().Hash(options.Password);
        var profile = new StaffProfile(options.FullName, PhoneNumber: string.Empty, Address: null);
        repository.Add(new StaffAccount(options.Email, StaffRole.Admin, profile, passwordHash, mustChangePassword: false));
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);

        logger.LogInformation("Bootstrap admin account created.");
    }
}
