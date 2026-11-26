using DentalAPI.Domain.Patients;

namespace DentalAPI.Application.Patients;

public sealed record PatientDto(
    Guid Id,
    string FullName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string? Email,
    string? MedicalNotes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static PatientDto FromEntity(Patient patient) => new(
        patient.Id,
        patient.FullName,
        patient.DateOfBirth,
        patient.PhoneNumber,
        patient.Email,
        patient.MedicalNotes,
        patient.CreatedAt,
        patient.UpdatedAt);
}
