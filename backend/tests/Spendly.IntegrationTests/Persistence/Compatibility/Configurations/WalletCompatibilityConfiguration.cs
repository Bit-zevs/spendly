using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Spendly.Domain.ValueObjects;
using Spendly.Domain.Wallets;
using Spendly.Infrastructure.Persistence.Converters;

namespace Spendly.IntegrationTests.Persistence.Compatibility.Configurations;

internal sealed class WalletCompatibilityConfiguration
    : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable(
            "wallets",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_wallets_currency_code_format",
                    "currency_code ~ '^[A-Z]{3}$'");

                tableBuilder.HasCheckConstraint(
                    "ck_wallets_type_defined",
                    "type IN (1, 2, 3, 4, 5, 6, 7)");
            });

        builder
            .HasKey(wallet => wallet.Id)
            .HasName("pk_wallets");

        builder
            .Property(wallet => wallet.Id)
            .HasConversion(new WalletIdConverter())
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder
            .Property(wallet => wallet.Name)
            .HasColumnName("name")
            .HasMaxLength(Wallet.MaxNameLength)
            .IsRequired();

        builder
            .Property(wallet => wallet.Type)
            .HasConversion(CompatibilityValueConverters.WalletTypeToInt16)
            .HasColumnName("type")
            .HasColumnType("smallint")
            .IsRequired();

        builder
            .Property(wallet => wallet.Currency)
            .HasConversion(CompatibilityValueConverters.CurrencyToCode)
            .HasColumnName("currency_code")
            .HasColumnType("character varying(3)")
            .HasMaxLength(Currency.CodeLength)
            .IsRequired();

        builder
            .Property(wallet => wallet.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
