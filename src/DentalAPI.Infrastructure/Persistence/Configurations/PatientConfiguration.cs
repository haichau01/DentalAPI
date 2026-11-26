using DentalAPI.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalAPI.Infrastructure.Persistence.Configurations;

public sealed class PatientConfiguration : EntityConfiguration<Patient>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");

        builder.Property(patient => patient.FullName).HasMaxLength(Patient.FullNameMaxLength).IsRequired();
        builder.Property(patient => patient.PhoneNumber).HasMaxLength(Patient.PhoneNumberMaxLength).IsRequired();
        builder.Property(patient => patient.Email).HasMaxLength(Patient.EmailMaxLength);
        builder.Property(patient => patient.MedicalNotes).HasMaxLength(Patient.MedicalNotesMaxLength);
        builder.OwnsOne(patient => patient.Address, OwnedTypeMappings.ConfigureAddress);

        builder.HasIndex(patient => patient.FullName);
        builder.HasIndex(patient => patient.PhoneNumber);
    }
}
