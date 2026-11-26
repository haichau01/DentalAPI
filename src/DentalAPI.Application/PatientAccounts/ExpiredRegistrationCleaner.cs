using DentalAPI.Application.Auth;
using DentalAPI.Domain.Accounts;

namespace DentalAPI.Application.PatientAccounts;

public sealed record RegistrationCleanupResult(int DeletedRegistrations, int DeletedVerificationCodes);

/// <summary>Removes personal data left behind by registrations that were never verified.</summary>
public sealed class ExpiredRegistrationCleaner(
    IPatientAccountRepository accountRepository,
    IVerificationCodeRepository verificationCodeRepository,
    TimeProvider timeProvider)
{
    /// <summary>Deletes pending registrations and verification codes older than <see cref="PatientAccount.PendingRegistrationLifetime"/>.</summary>
    public async Task<RegistrationCleanupResult> PurgeAsync(CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow() - PatientAccount.PendingRegistrationLifetime;

        var deletedRegistrations = await accountRepository.DeletePendingNotUpdatedSinceAsync(cutoff, cancellationToken);
        var deletedCodes = await verificationCodeRepository.DeleteSentBeforeAsync(cutoff, cancellationToken);

        return new RegistrationCleanupResult(deletedRegistrations, deletedCodes);
    }
}
