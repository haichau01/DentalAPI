namespace DentalAPI.Domain.Authorization;

/// <summary>
/// Stable string codes for permissions, used in the API and the database.
/// Codes are fixed so renaming an enum member never breaks stored data or API clients.
/// </summary>
public static class PermissionCodes
{
    public const int CodeMaxLength = 64;

    private static readonly Dictionary<Permission, string> _codesByPermission = new()
    {
        [Permission.PatientsRead] = "patients.read",
        [Permission.PatientsWrite] = "patients.write",
        [Permission.PatientsDelete] = "patients.delete",
        [Permission.AppointmentsRead] = "appointments.read",
        [Permission.AppointmentsWrite] = "appointments.write",
        [Permission.TreatmentRecordsRead] = "treatment-records.read",
        [Permission.TreatmentRecordsWrite] = "treatment-records.write",
        [Permission.InvoicesRead] = "invoices.read",
        [Permission.InvoicesWrite] = "invoices.write",
        [Permission.ServicesManage] = "services.manage",
    };

    private static readonly Dictionary<string, Permission> _permissionsByCode =
        _codesByPermission.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<Permission> All => _codesByPermission.Keys;

    public static string ToCode(Permission permission) => _codesByPermission[permission];

    public static Permission FromCode(string code) => _permissionsByCode[code];

    public static bool TryFromCode(string? code, out Permission permission)
    {
        permission = default;
        return code is not null && _permissionsByCode.TryGetValue(code, out permission);
    }
}
