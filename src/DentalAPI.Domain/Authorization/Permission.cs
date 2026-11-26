namespace DentalAPI.Domain.Authorization;

/// <summary>
/// What a staff member may do. Permissions for modules that do not exist yet are declared now
/// so admins can configure them before those endpoints ship.
/// </summary>
public enum Permission
{
    PatientsRead,
    PatientsWrite,
    PatientsDelete,
    AppointmentsRead,
    AppointmentsWrite,
    TreatmentRecordsRead,
    TreatmentRecordsWrite,
    InvoicesRead,
    InvoicesWrite,
    ServicesManage,
}

public enum PermissionEffect
{
    Grant,
    Revoke,
}
