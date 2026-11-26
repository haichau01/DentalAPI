using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalAPI.Infrastructure.Persistence.Configurations;

public sealed class PatientAccountConfiguration : EntityConfiguration<PatientAccount>
{
    private const int StatusMaxLength = 32;

    protected override void ConfigureEntity(EntityTypeBuilder<PatientAccount> builder)
    {
        builder.ToTable("PatientAccounts");

        builder.Property(account => account.PhoneNumber).HasMaxLength(PatientAccount.PhoneNumberMaxLength).IsRequired();
        builder.Property(account => account.Email).HasMaxLength(PatientAccount.EmailMaxLength);
        builder.Property(account => account.PasswordHash).HasMaxLength(PatientAccount.PasswordHashMaxLength).IsRequired();
        builder.Property(account => account.Status).HasConversion<string>().HasMaxLength(StatusMaxLength);
        OwnedTypeMappings.ConfigureLockout(builder);

        builder.OwnsOne(account => account.PendingProfile, profile =>
        {
            profile.Property(item => item.FullName).HasMaxLength(Patient.FullNameMaxLength).IsRequired();
            profile.Property(item => item.DateOfBirth).IsRequired();
            profile.OwnsOne(item => item.Address, OwnedTypeMappings.ConfigureAddress);
        });

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(account => account.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // These indexes are the last line of defence when two registrations race each other.
        builder.HasIndex(account => account.PhoneNumber)
            .IsUnique()
            .HasFilter($"[IsDeleted] = 0 AND [Status] = '{nameof(PatientAccountStatus.Active)}'");
        builder.HasIndex(account => account.Email)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [IsEmailVerified] = 1");
        builder.HasIndex(account => account.PatientId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0 AND [PatientId] IS NOT NULL");
    }
}
