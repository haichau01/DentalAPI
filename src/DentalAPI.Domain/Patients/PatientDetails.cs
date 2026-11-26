namespace DentalAPI.Domain.Patients;

/// <summary>The editable details of a patient, grouped so constructors and updates take one argument.</summary>
public sealed record PatientDetails(
    string FullName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string? Email,
    string? MedicalNotes);
