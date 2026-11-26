using DentalAPI.Application.Common.Exceptions;
using DentalAPI.Domain.Accounts;

namespace DentalAPI.Application.Common.Security;

public static class CurrentUserExtensions
{
    /// <summary>Returns the caller's patient record id.</summary>
    /// <exception cref="ForbiddenException">The caller is not a patient linked to a record.</exception>
    public static Guid GetRequiredPatientId(this ICurrentUser currentUser) =>
        currentUser.AccountType == AccountType.Patient && currentUser.PatientId is { } patientId
            ? patientId
            : throw new ForbiddenException();

    /// <summary>Returns the caller's account id when it is of the expected type.</summary>
    /// <exception cref="ForbiddenException">The caller is anonymous or a different kind of account.</exception>
    public static Guid GetRequiredAccountId(this ICurrentUser currentUser, AccountType accountType) =>
        currentUser.AccountType == accountType && currentUser.AccountId is { } accountId
            ? accountId
            : throw new ForbiddenException();
}
