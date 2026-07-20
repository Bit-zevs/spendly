using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Spendly.Domain.Wallets;
using Spendly.Infrastructure.Persistence.Converters;

namespace Spendly.Infrastructure.Persistence.Configuration;

internal sealed class WalletConfiguration
    : IEntityTypeConfiguration<Wallet>
{
    private const string WalletTypeCheckConstraintSql =
        "type IN (1, 2, 3, 4, 5, 6, 7)";

    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable(
            "wallets",
            tableBuilder =>
            {
                tableBuilder.HasCurrencyCodeCheckConstraint(
                    "ck_wallets_currency_code_format",
                    "currency_code");

                tableBuilder.HasCheckConstraint(
                    "ck_wallets_type_defined",
                    WalletTypeCheckConstraintSql);
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
            .HasConversion(new WalletTypeConverter())
            .HasColumnName("type")
            .HasColumnType("smallint")
            .IsRequired();

        builder
            .Property(wallet => wallet.Currency)
            .HasCurrencyCodeMapping("currency_code");

        builder
            .Property(wallet => wallet.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
