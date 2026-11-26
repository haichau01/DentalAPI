using DentalAPI.Application.Common.Addresses;
using DentalAPI.Domain.Patients;

namespace DentalAPI.Application.Patients;

public sealed record PatientDto(
    Guid Id,
    string FullName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string? Email,
    string? MedicalNotes,
    AddressDto? Address,
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
        AddressDto.FromDomain(patient.Address),
        patient.CreatedAt,
        patient.UpdatedAt);
}
