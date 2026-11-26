using DentalAPI.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalAPI.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : EntityConfiguration<RefreshToken>
{
    private const int AccountTypeMaxLength = 16;

    protected override void ConfigureEntity(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.Property(token => token.TokenHash).HasMaxLength(RefreshToken.TokenHashMaxLength).IsRequired();
        builder.Property(token => token.AccountType).HasConversion<string>().HasMaxLength(AccountTypeMaxLength);

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.AccountId);
    }
}

public sealed class VerificationCodeConfiguration : EntityConfiguration<VerificationCode>
{
    private const int ChannelMaxLength = 16;

    protected override void ConfigureEntity(EntityTypeBuilder<VerificationCode> builder)
    {
        builder.ToTable("VerificationCodes");

        builder.Property(code => code.Channel).HasConversion<string>().HasMaxLength(ChannelMaxLength);
        builder.Property(code => code.Destination).HasMaxLength(VerificationCode.DestinationMaxLength).IsRequired();
        builder.Property(code => code.CodeHash).HasMaxLength(VerificationCode.CodeHashMaxLength).IsRequired();

        builder.HasIndex(code => new { code.Channel, code.Destination, code.SentAt });
    }
}
