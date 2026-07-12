using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Spendly.Domain.ValueObjects;
using Spendly.Domain.Wallets;

namespace Spendly.IntegrationTests.Persistence.Compatibility.Configurations;

internal sealed class WalletCompatibilityConfiguration
    : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets");

        builder
            .HasKey(wallet => wallet.Id)
            .HasName("pk_wallets");

        builder
            .Property(wallet => wallet.Id)
            .HasConversion(CompatibilityValueConverters.WalletIdToGuid)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder
            .Property(wallet => wallet.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();

        builder
            .Property(wallet => wallet.Type)
            .HasColumnName("type")
            .HasColumnType("integer")
            .IsRequired();

        builder
            .Property(wallet => wallet.Currency)
            .HasConversion(CompatibilityValueConverters.CurrencyToCode)
            .HasColumnName("currency_code")
            .HasMaxLength(Currency.CodeLength)
            .IsRequired();

        builder
            .Property(wallet => wallet.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
