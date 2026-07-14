using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Spendly.Domain.Categories;
using Spendly.Domain.Transactions;
using Spendly.Domain.ValueObjects;
using Spendly.Domain.Wallets;

namespace Spendly.IntegrationTests.Persistence.Compatibility.Configurations;

internal sealed class TransactionCompatibilityConfiguration
    : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable(
            "transactions",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_transactions_amount_positive",
                    "amount > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_transactions_currency_code_format",
                    "currency_code ~ '^[A-Z]{3}$'");

                tableBuilder.HasCheckConstraint(
                    "ck_transactions_type_defined",
                    "type IN (1, 2, 3)");
            });

        builder
            .HasKey(transaction => transaction.Id)
            .HasName("pk_transactions");

        builder
            .Property(transaction => transaction.Id)
            .HasConversion(CompatibilityValueConverters.TransactionIdToGuid)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder
            .Property(transaction => transaction.Type)
            .HasConversion(CompatibilityValueConverters.TransactionTypeToInt16)
            .HasColumnName("type")
            .HasColumnType("smallint")
            .IsRequired();

        ConfigureAmount(builder);

        builder
            .Property(transaction => transaction.WalletId)
            .HasConversion(CompatibilityValueConverters.WalletIdToGuid)
            .HasColumnName("wallet_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder
            .Property(transaction => transaction.CategoryId)
            .HasConversion(CompatibilityValueConverters.CategoryIdToGuid)
            .HasColumnName("category_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder
            .Property(transaction => transaction.OccurredAt)
            .HasColumnName("occurred_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder
            .Property(transaction => transaction.Description)
            .HasColumnName("description")
            .HasMaxLength(Transaction.MaxDescriptionLength);

        builder
            .Property(transaction => transaction.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder
            .HasIndex(transaction => transaction.WalletId)
            .HasDatabaseName("ix_transactions_wallet_id");

        builder
            .HasIndex(transaction => transaction.CategoryId)
            .HasDatabaseName("ix_transactions_category_id");

        builder
            .HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(transaction => transaction.WalletId)
            .HasConstraintName("fk_transactions_wallets_wallet_id")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder
            .HasOne<Category>()
            .WithMany()
            .HasForeignKey(transaction => transaction.CategoryId)
            .HasConstraintName("fk_transactions_categories_category_id")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }

    private static void ConfigureAmount(
        EntityTypeBuilder<Transaction> builder)
    {
        var amountBuilder = builder.ComplexProperty(
            transaction => transaction.Amount);

        amountBuilder
            .IsRequired()
            .HasField("_amount")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        amountBuilder
            .Property(money => money.Amount)
            .HasField("_amount")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("amount")
            .HasColumnType("numeric(19,4)")
            .HasPrecision(Money.Precision, Money.Scale)
            .IsRequired();

        amountBuilder
            .Property(money => money.Currency)
            .HasField("_currency")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(CompatibilityValueConverters.CurrencyToCode)
            .HasColumnName("currency_code")
            .HasColumnType("character varying(3)")
            .HasMaxLength(Currency.CodeLength)
            .IsRequired();
    }
}
