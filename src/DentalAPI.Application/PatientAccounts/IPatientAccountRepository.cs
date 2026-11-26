using DentalAPI.Domain.Accounts;

namespace DentalAPI.Application.PatientAccounts;

/// <summary>Loads and stores patient accounts. Phone numbers and emails passed in must already be normalized.</summary>
public interface IPatientAccountRepository
{
    Task<PatientAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PatientAccount?> GetActiveByPhoneAsync(string phoneNumber, CancellationToken cancellationToken);

    Task<PatientAccount?> GetPendingByPhoneAsync(string phoneNumber, CancellationToken cancellationToken);

    /// <summary>Finds an active account by email, only when that email has been verified.</summary>
    Task<PatientAccount?> GetActiveByVerifiedEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>True when another account already uses this email as its verified sign-in email.</summary>
    Task<bool> IsVerifiedEmailInUseAsync(string email, Guid excludeAccountId, CancellationToken cancellationToken);

    /// <summary>
    /// Permanently deletes registrations still pending verification that were last changed before the cutoff.
    /// A hard delete on purpose: the person never proved they own the phone, so their details must not be kept.
    /// </summary>
    /// <returns>The number of deleted registrations.</returns>
    Task<int> DeletePendingNotUpdatedSinceAsync(DateTimeOffset cutoff, CancellationToken cancellationToken);

    void Add(PatientAccount account);

    /// <summary>Marks the account for deletion; the persistence layer turns this into a soft delete.</summary>
    void Remove(PatientAccount account);
}
