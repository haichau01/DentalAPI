namespace DentalAPI.Domain.Accounts;

/// <summary>Patients and staff sign in separately and are stored in separate tables.</summary>
public enum AccountType
{
    Staff,
    Patient,
}

public enum StaffRole
{
    /// <summary>Always holds every permission and is the only role that manages accounts and permissions.</summary>
    Admin,
    Dentist,
    Receptionist,
}

public enum PatientAccountStatus
{
    /// <summary>Registered but the phone number is not verified yet; cannot sign in.</summary>
    PendingVerification,
    Active,
}
