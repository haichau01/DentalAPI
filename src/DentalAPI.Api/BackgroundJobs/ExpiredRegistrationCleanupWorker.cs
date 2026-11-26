using DentalAPI.Application.PatientAccounts;

namespace DentalAPI.Api.BackgroundJobs;

/// <summary>Runs <see cref="ExpiredRegistrationCleaner"/> at startup and then every hour.</summary>
public sealed class ExpiredRegistrationCleanupWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ExpiredRegistrationCleanupWorker> logger) : BackgroundService
{
    private static readonly TimeSpan _interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        do
        {
            await PurgeOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PurgeOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var cleaner = scope.ServiceProvider.GetRequiredService<ExpiredRegistrationCleaner>();
            var result = await cleaner.PurgeAsync(stoppingToken);

            logger.LogInformation(
                "Removed {Registrations} expired registrations and {Codes} expired verification codes.",
                result.DeletedRegistrations,
                result.DeletedVerificationCodes);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A failed run (e.g. database briefly down) must not stop the host; the next run retries.
            logger.LogError(exception, "Expired registration cleanup failed; it will retry on the next run.");
        }
    }
}
