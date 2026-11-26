using DentalAPI.Domain.Accounts;

namespace DentalAPI.Application.Common.Security;

/// <summary>Who an access token is issued for. Permissions are deliberately not included so changes apply immediately.</summary>
public sealed record AccessTokenSubject(
    Guid AccountId,
    AccountType AccountType,
    string Role,
    Guid? PatientId,
    bool IsPasswordChangeRequired)
{
    public static AccessTokenSubject ForStaff(StaffAccount account) =>
        new(account.Id, AccountType.Staff, account.Role.ToString(), PatientId: null, account.MustChangePassword);

    public static AccessTokenSubject ForPatient(PatientAccount account) =>
        new(account.Id, AccountType.Patient, AccessTokenClaims.PatientRole, account.PatientId, IsPasswordChangeRequired: false);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
