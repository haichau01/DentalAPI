using DentalAPI.Domain.Patients;

namespace DentalAPI.Application.Patients;

/// <summary>Request body for both creating and updating a patient.</summary>
public sealed record SavePatientRequest(
    string FullName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string? Email,
    string? MedicalNotes)
{
    public PatientDetails ToDetails() => new(FullName, DateOfBirth, PhoneNumber, Email, MedicalNotes);
}
