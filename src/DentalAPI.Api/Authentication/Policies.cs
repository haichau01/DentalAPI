namespace DentalAPI.Api.Authentication;

/// <summary>Named authorization policies. Staff endpoints use <see cref="RequirePermissionAttribute"/> instead of a named policy.</summary>
public static class Policies
{
    /// <summary>Signed-in admin who is not waiting to change an admin-chosen password.</summary>
    public const string Admin = nameof(Admin);

    /// <summary>Signed-in patient linked to a patient record; for the /me endpoints.</summary>
    public const string PatientSelf = nameof(PatientSelf);
}
