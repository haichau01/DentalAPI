using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalAPI.Infrastructure.Persistence.Configurations;

public sealed class StaffRolePermissionConfiguration : IEntityTypeConfiguration<StaffRolePermission>
{
    private const int RoleMaxLength = 32;

    public void Configure(EntityTypeBuilder<StaffRolePermission> builder)
    {
        builder.ToTable("StaffRolePermissions");

        builder.HasKey(item => new { item.Role, item.Permission });
        builder.Property(item => item.Role).HasConversion<string>().HasMaxLength(RoleMaxLength);
        builder.Property(item => item.Permission)
            .HasConversion(OwnedTypeMappings.PermissionConverter)
            .HasMaxLength(PermissionCodes.CodeMaxLength);

        // Default permissions are configuration, not patient data, so they ship with the migration.
        builder.HasData(DefaultStaffRolePermissions.All);
    }
}

public sealed class StaffPermissionOverrideConfiguration : EntityConfiguration<StaffPermissionOverride>
{
    private const int EffectMaxLength = 16;

    protected override void ConfigureEntity(EntityTypeBuilder<StaffPermissionOverride> builder)
    {
        builder.ToTable("StaffPermissionOverrides");

        builder.Property(item => item.Permission)
            .HasConversion(OwnedTypeMappings.PermissionConverter)
            .HasMaxLength(PermissionCodes.CodeMaxLength);
        builder.Property(item => item.Effect).HasConversion<string>().HasMaxLength(EffectMaxLength);

        builder.HasOne<StaffAccount>()
            .WithMany()
            .HasForeignKey(item => item.StaffAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => new { item.StaffAccountId, item.Permission })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
