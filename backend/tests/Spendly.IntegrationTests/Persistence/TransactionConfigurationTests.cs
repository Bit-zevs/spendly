using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Spendly.Domain.Categories;
using Spendly.Domain.Transactions;
using Spendly.Domain.ValueObjects;
using Spendly.Domain.Wallets;
using Spendly.Infrastructure.Persistence;

namespace Spendly.IntegrationTests.Persistence;

public sealed class TransactionConfigurationTests
{
    [Fact]
    public void Mapping_ShouldUseApprovedPostgreSqlContract()
    {
        using var context = CreateContext();

        var model = context.GetService<IDesignTimeModel>().Model;
        var entityType = model.FindEntityType(typeof(Transaction));

        Assert.NotNull(entityType);
        Assert.Equal("transactions", entityType.GetTableName());
        Assert.Equal(
            "pk_transactions",
            entityType.FindPrimaryKey()?.GetName());

        var table = StoreObjectIdentifier.Table(
            entityType.GetTableName()!,
            entityType.GetSchema());

        AssertProperty(
            entityType,
            table,
            nameof(Transaction.Id),
            "id",
            isNullable: false,
            columnType: "uuid",
            providerClrType: typeof(Guid));

        var idProperty = entityType.FindProperty(nameof(Transaction.Id));

        Assert.NotNull(idProperty);
        Assert.Equal(ValueGenerated.Never, idProperty.ValueGenerated);

        AssertProperty(
            entityType,
            table,
            nameof(Transaction.Type),
            "type",
            isNullable: false,
            columnType: "smallint",
            providerClrType: typeof(short));

        AssertMoney(entityType, table);

        AssertProperty(
            entityType,
            table,
            nameof(Transaction.WalletId),
            "wallet_id",
            isNullable: false,
            columnType: "uuid",
            providerClrType: typeof(Guid));

        AssertProperty(
            entityType,
            table,
            nameof(Transaction.CategoryId),
            "category_id",
            isNullable: false,
            columnType: "uuid",
            providerClrType: typeof(Guid));

        AssertProperty(
            entityType,
            table,
            nameof(Transaction.OccurredAt),
            "occurred_at",
            isNullable: false,
            columnType: "timestamp with time zone");

        AssertProperty(
            entityType,
            table,
            nameof(Transaction.Description),
            "description",
            isNullable: true);

        var descriptionProperty = entityType.FindProperty(
            nameof(Transaction.Description));

        Assert.NotNull(descriptionProperty);
        Assert.Equal(
            Transaction.MaxDescriptionLength,
            descriptionProperty.GetMaxLength());

        AssertProperty(
            entityType,
            table,
            nameof(Transaction.CreatedAt),
            "created_at",
            isNullable: false,
            columnType: "timestamp with time zone");

        AssertProperty(
            entityType,
            table,
            nameof(Transaction.UpdatedAt),
            "updated_at",
            isNullable: true,
            columnType: "timestamp with time zone");

        AssertCheckConstraint(
            entityType,
            "ck_transactions_amount_positive",
            "amount > 0");

        AssertCheckConstraint(
            entityType,
            "ck_transactions_currency_code_format",
            "currency_code ~ '^[A-Z]{3}$'");

        AssertCheckConstraint(
            entityType,
            "ck_transactions_type_defined",
            "type IN (1, 2, 3)");

        AssertForeignKey(
            entityType,
            typeof(Wallet),
            nameof(Transaction.WalletId),
            "fk_transactions_wallets_wallet_id");

        AssertForeignKey(
            entityType,
            typeof(Category),
            nameof(Transaction.CategoryId),
            "fk_transactions_categories_category_id");

        AssertIndex(
            entityType,
            "ix_transactions_wallet_id",
            nameof(Transaction.WalletId));

        AssertIndex(
            entityType,
            "ix_transactions_category_id",
            nameof(Transaction.CategoryId));

        AssertIndex(
            entityType,
            "ix_transactions_occurred_at",
            nameof(Transaction.OccurredAt));
    }

    [Fact]
    public void Mapping_ShouldMatchCurrentDomainShape()
    {
        using var context = CreateContext();

        var model = context.GetService<IDesignTimeModel>().Model;
        var entityType = model.FindEntityType(typeof(Transaction));

        Assert.NotNull(entityType);

        var propertyNames = entityType
            .GetProperties()
            .Select(property => property.Name)
            .Order()
            .ToArray();

        string[] expectedPropertyNames =
        [
            nameof(Transaction.CategoryId),
            nameof(Transaction.CreatedAt),
            nameof(Transaction.Description),
            nameof(Transaction.Id),
            nameof(Transaction.OccurredAt),
            nameof(Transaction.Type),
            nameof(Transaction.UpdatedAt),
            nameof(Transaction.WalletId)
        ];

        Assert.Equal(expectedPropertyNames, propertyNames);

        var complexPropertyNames = entityType
            .GetComplexProperties()
            .Select(property => property.Name)
            .Order()
            .ToArray();

        string[] expectedComplexPropertyNames =
        [
            nameof(Transaction.Amount)
        ];

        Assert.Equal(
            expectedComplexPropertyNames,
            complexPropertyNames);
        Assert.Equal(2, entityType.GetForeignKeys().Count());
        Assert.Equal(3, entityType.GetIndexes().Count());
        Assert.Empty(entityType.GetNavigations());
    }

    private static void AssertMoney(
        IEntityType entityType,
        StoreObjectIdentifier table)
    {
        var moneyProperty = entityType.FindComplexProperty(
            nameof(Transaction.Amount));

        Assert.NotNull(moneyProperty);

        var amountProperty = moneyProperty.ComplexType.FindProperty(
            nameof(Money.Amount));

        Assert.NotNull(amountProperty);
        Assert.Equal("amount", amountProperty.GetColumnName(table));
        Assert.Equal(
            $"numeric({Money.Precision},{Money.Scale})",
            amountProperty.GetColumnType());
        Assert.Equal(Money.Precision, amountProperty.GetPrecision());
        Assert.Equal(Money.Scale, amountProperty.GetScale());
        Assert.False(amountProperty.IsNullable);

        var currencyProperty = moneyProperty.ComplexType.FindProperty(
            nameof(Money.Currency));

        Assert.NotNull(currencyProperty);
        Assert.Equal(
            "currency_code",
            currencyProperty.GetColumnName(table));
        Assert.Equal(
            $"character varying({Currency.CodeLength})",
            currencyProperty.GetColumnType());
        Assert.Equal(
            Currency.CodeLength,
            currencyProperty.GetMaxLength());
        Assert.Equal(
            typeof(string),
            currencyProperty.GetValueConverter()?.ProviderClrType);
        Assert.False(currencyProperty.IsNullable);
    }

    private static void AssertProperty(
        IEntityType entityType,
        StoreObjectIdentifier table,
        string propertyName,
        string columnName,
        bool isNullable,
        string? columnType = null,
        Type? providerClrType = null)
    {
        var property = entityType.FindProperty(propertyName);

        Assert.NotNull(property);
        Assert.Equal(columnName, property.GetColumnName(table));
        Assert.Equal(isNullable, property.IsNullable);

        if (columnType is not null)
        {
            Assert.Equal(columnType, property.GetColumnType());
        }

        if (providerClrType is not null)
        {
            Assert.Equal(
                providerClrType,
                property.GetValueConverter()?.ProviderClrType);
        }
    }

    private static void AssertCheckConstraint(
        IEntityType entityType,
        string name,
        string sql)
    {
        var checkConstraint = entityType
            .GetCheckConstraints()
            .Single(constraint => constraint.Name == name);

        Assert.Equal(sql, checkConstraint.Sql);
    }

    private static void AssertForeignKey(
        IEntityType entityType,
        Type principalType,
        string propertyName,
        string constraintName)
    {
        var foreignKey = entityType
            .GetForeignKeys()
            .Single(candidate =>
                candidate.PrincipalEntityType.ClrType == principalType);

        Assert.True(foreignKey.IsRequired);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        Assert.Equal(constraintName, foreignKey.GetConstraintName());
        Assert.Equal(
            new[] { propertyName },
            foreignKey.Properties
                .Select(property => property.Name)
                .ToArray());
    }

    private static void AssertIndex(
        IEntityType entityType,
        string databaseName,
        string propertyName)
    {
        var index = entityType
            .GetIndexes()
            .Single(candidate =>
                candidate.GetDatabaseName() == databaseName);

        Assert.Equal(
            new[] { propertyName },
            index.Properties
                .Select(property => property.Name)
                .ToArray());
    }

    private static TransactionConfigurationTestDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<TransactionConfigurationTestDbContext>()
                .UseNpgsql(
                    "Host=localhost;Database=spendly_transaction_mapping_tests;" +
                    "Username=spendly;Password=spendly")
                .Options;

        return new TransactionConfigurationTestDbContext(options);
    }

    private sealed class TransactionConfigurationTestDbContext(
        DbContextOptions<TransactionConfigurationTestDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(SpendlyDbContext).Assembly);
        }
    }
}
