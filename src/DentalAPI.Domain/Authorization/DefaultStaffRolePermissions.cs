using DentalAPI.Domain.Accounts;

namespace DentalAPI.Domain.Authorization;

/// <summary>
/// The permissions each role starts with. Admins can change them later; these are only the seed.
/// Dentists record treatment but do not edit patient contact data or invoices; receptionists handle
/// patients, appointments and billing but do not see treatment records.
/// </summary>
public static class DefaultStaffRolePermissions
{
    public static IReadOnlyList<StaffRolePermission> All { get; } =
    [
        new(StaffRole.Dentist, Permission.PatientsRead),
        new(StaffRole.Dentist, Permission.AppointmentsRead),
        new(StaffRole.Dentist, Permission.TreatmentRecordsRead),
        new(StaffRole.Dentist, Permission.TreatmentRecordsWrite),

        new(StaffRole.Receptionist, Permission.PatientsRead),
        new(StaffRole.Receptionist, Permission.PatientsWrite),
        new(StaffRole.Receptionist, Permission.AppointmentsRead),
        new(StaffRole.Receptionist, Permission.AppointmentsWrite),
        new(StaffRole.Receptionist, Permission.InvoicesRead),
        new(StaffRole.Receptionist, Permission.InvoicesWrite),
    ];
}
