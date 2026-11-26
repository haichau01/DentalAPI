using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Authorization;
using DentalAPI.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DentalAPI.Infrastructure.Persistence.Configurations;

/// <summary>Mappings shared by several entities, so the same value object gets the same columns everywhere.</summary>
internal static class OwnedTypeMappings
{
    public static readonly ValueConverter<Permission, string> PermissionConverter =
        new(permission => PermissionCodes.ToCode(permission), code => PermissionCodes.FromCode(code));

    /// <summary>
    /// Address is optional; the province is required inside it, which is how EF Core tells
    /// "no address" (all columns null) apart from an address with only a province.
    /// </summary>
    public static void ConfigureAddress<TOwner>(OwnedNavigationBuilder<TOwner, Address> address)
        where TOwner : class
    {
        address.Property(item => item.StreetLine).HasMaxLength(Address.StreetLineMaxLength);
        address.Property(item => item.Ward).HasMaxLength(Address.WardMaxLength);
        address.Property(item => item.Province).HasMaxLength(Address.ProvinceMaxLength).IsRequired();
    }

    public static void ConfigureLockout<TOwner>(EntityTypeBuilder<TOwner> builder)
        where TOwner : class, IPasswordCredential
    {
        builder.OwnsOne(account => account.Lockout, lockout =>
        {
            lockout.Property(item => item.FailedCount).HasColumnName("FailedLoginCount");
            lockout.Property(item => item.LockedUntil).HasColumnName("LockedUntil");
        });
        builder.Navigation(account => account.Lockout).IsRequired();
    }
}
