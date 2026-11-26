using DentalAPI.Domain.Common;
using DentalAPI.Domain.Patients;

namespace DentalAPI.Domain.Accounts;

/// <summary>
/// Details typed in at registration. They are kept on the account until the phone number is verified,
/// because only then is it known whether the account joins an existing patient record or needs a new one.
/// </summary>
public sealed class PendingPatientProfile
{
    // Required by EF Core to materialize owned types from the database.
    private PendingPatientProfile()
    {
    }

    public PendingPatientProfile(string fullName, DateOnly dateOfBirth, Address? address)
    {
        FullName = fullName.Trim();
        DateOfBirth = dateOfBirth;
        Address = address;
    }

    public string FullName { get; private set; } = string.Empty;

    public DateOnly DateOfBirth { get; private set; }

    public Address? Address { get; private set; }

    public PatientDetails ToPatientDetails(string phoneNumber, string? email) =>
        new(FullName, DateOfBirth, phoneNumber, email, MedicalNotes: null, Address);
}
