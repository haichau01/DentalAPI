using DentalAPI.Application.Common.Addresses;
using DentalAPI.Domain.Patients;

namespace DentalAPI.Application.Patients;

/// <summary>
/// A patient's own record as shown to the patient. <see cref="Patient.MedicalNotes"/> is left out: it holds clinical
/// notes written by staff for staff, and showing them to patients is a clinic policy decision not made yet.
/// </summary>
public sealed record PatientProfileDto(
    Guid Id,
    string FullName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string? Email,
    AddressDto? Address,
    DateTimeOffset UpdatedAt)
{
    public static PatientProfileDto FromEntity(Patient patient) => new(
        patient.Id,
        patient.FullName,
        patient.DateOfBirth,
        patient.PhoneNumber,
        patient.Email,
        AddressDto.FromDomain(patient.Address),
        patient.UpdatedAt);
}
