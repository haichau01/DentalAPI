using DentalAPI.Domain.Common;

namespace DentalAPI.Domain.Patients;

/// <summary>A clinic patient. Contact details and medical notes are sensitive data and must never be logged.</summary>
public sealed class Patient : Entity
{
    public const int FullNameMaxLength = 200;
    public const int PhoneNumberMaxLength = 20;
    public const int EmailMaxLength = 256;
    public const int MedicalNotesMaxLength = 4000;

    // Required by EF Core to materialize entities from the database.
    private Patient()
    {
    }

    public Patient(PatientDetails details)
    {
        UpdateDetails(details);
    }

    public string FullName { get; private set; } = string.Empty;

    public DateOnly DateOfBirth { get; private set; }

    public string PhoneNumber { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    /// <summary>Medical history and allergies the dentist must know before treatment.</summary>
    public string? MedicalNotes { get; private set; }

    public void UpdateDetails(PatientDetails details)
    {
        FullName = details.FullName.Trim();
        DateOfBirth = details.DateOfBirth;
        PhoneNumber = details.PhoneNumber.Trim();
        Email = details.Email?.Trim();
        MedicalNotes = details.MedicalNotes?.Trim();
    }
}
