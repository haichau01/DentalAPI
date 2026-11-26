using DentalAPI.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalAPI.Infrastructure.Persistence.Configurations;

public sealed class StaffAccountConfiguration : EntityConfiguration<StaffAccount>
{
    private const int RoleMaxLength = 32;

    protected override void ConfigureEntity(EntityTypeBuilder<StaffAccount> builder)
    {
        builder.ToTable("StaffAccounts");

        builder.Property(account => account.Email).HasMaxLength(StaffAccount.EmailMaxLength).IsRequired();
        builder.Property(account => account.FullName).HasMaxLength(StaffAccount.FullNameMaxLength).IsRequired();
        builder.Property(account => account.PhoneNumber).HasMaxLength(StaffAccount.PhoneNumberMaxLength).IsRequired();
        builder.Property(account => account.PasswordHash).HasMaxLength(StaffAccount.PasswordHashMaxLength).IsRequired();
        builder.Property(account => account.Role).HasConversion<string>().HasMaxLength(RoleMaxLength);
        builder.OwnsOne(account => account.Address, OwnedTypeMappings.ConfigureAddress);
        OwnedTypeMappings.ConfigureLockout(builder);

        // Deleted accounts keep their row, so the email may be reused by a new account.
        builder.HasIndex(account => account.Email).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(account => account.FullName);
    }
}
