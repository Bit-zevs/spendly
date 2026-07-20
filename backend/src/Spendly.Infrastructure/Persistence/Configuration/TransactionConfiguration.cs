using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Spendly.Domain.Categories;
using Spendly.Domain.Transactions;
using Spendly.Domain.Wallets;
using Spendly.Infrastructure.Persistence.Converters;

namespace Spendly.Infrastructure.Persistence.Configuration;

internal sealed class TransactionConfiguration
    : IEntityTypeConfiguration<Transaction>
{
    private const string TransactionTypeCheckConstraintSql =
        "type IN (1, 2, 3)";

    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable(
            "transactions",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_transactions_amount_positive",
                    "amount > 0");

                tableBuilder.HasCurrencyCodeCheckConstraint(
                    "ck_transactions_currency_code_format",
                    "currency_code");

                tableBuilder.HasCheckConstraint(
                    "ck_transactions_type_defined",
                    TransactionTypeCheckConstraintSql);
            });

        builder
            .HasKey(transaction => transaction.Id)
            .HasName("pk_transactions");

        builder
            .Property(transaction => transaction.Id)
            .HasConversion(new TransactionIdConverter())
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder
            .Property(transaction => transaction.Type)
            .HasConversion(new TransactionTypeConverter())
            .HasColumnName("type")
            .HasColumnType("smallint")
            .IsRequired();

        builder
            .ComplexProperty(transaction => transaction.Amount)
            .HasMoneyMapping(
                moneyBackingFieldName: "_amount",
                amountColumnName: "amount",
                currencyColumnName: "currency_code");

        builder
            .Property(transaction => transaction.WalletId)
            .HasConversion(new WalletIdConverter())
            .HasColumnName("wallet_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder
            .Property(transaction => transaction.CategoryId)
            .HasConversion(new CategoryIdConverter())
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
            .HasMaxLength(Transaction.MaxDescriptionLength)
            .IsRequired(false);

        builder
            .Property(transaction => transaction.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder
            .Property(transaction => transaction.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

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

        builder
            .HasIndex(transaction => transaction.WalletId)
            .HasDatabaseName("ix_transactions_wallet_id");

        builder
            .HasIndex(transaction => transaction.CategoryId)
            .HasDatabaseName("ix_transactions_category_id");

        builder
            .HasIndex(transaction => transaction.OccurredAt)
            .HasDatabaseName("ix_transactions_occurred_at");
    }
}
