namespace DentalAPI.Application.Common.Security;

/// <summary>Claim names in the access token, shared by the token issuer and the code that reads it.</summary>
public static class AccessTokenClaims
{
    public const string Subject = "sub";
    public const string TokenId = "jti";
    public const string AccountType = "account_type";
    public const string Role = "role";
    public const string PatientId = "patient_id";

    /// <summary>Present (value "true") only while a staff member still has to replace an admin-chosen password.</summary>
    public const string PasswordChangeRequired = "pwd_change_required";

    /// <summary>Role claim value for patient accounts; staff use their <see cref="Domain.Accounts.StaffRole"/> name.</summary>
    public const string PatientRole = "Patient";
}
