using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Spendly.Domain.Categories;
using Spendly.Domain.Transactions;
using Spendly.Domain.ValueObjects;
using Spendly.Domain.Wallets;

namespace Spendly.IntegrationTests.Persistence.Compatibility;

public sealed class EfCorePersistenceStrategyTests
{
    [Fact]
    public void Model_ShouldUseApprovedPostgreSqlTypesAndConverters()
    {
        using var context = CreateContext();

        var model = GetDesignTimeModel(context);

        var wallet = GetEntityType<Wallet>(model);
        var category = GetEntityType<Category>(model);
        var transaction = GetEntityType<Transaction>(model);

        Assert.Equal("wallets", wallet.GetTableName());
        Assert.Equal("categories", category.GetTableName());
        Assert.Equal("transactions", transaction.GetTableName());

        AssertIdentifier(
            wallet,
            nameof(Wallet.Id),
            "id",
            typeof(Guid));

        AssertIdentifier(
            category,
            nameof(Category.Id),
            "id",
            typeof(Guid));

        AssertIdentifier(
            transaction,
            nameof(Transaction.Id),
            "id",
            typeof(Guid));

        AssertIdentifier(
            transaction,
            nameof(Transaction.WalletId),
            "wallet_id",
            typeof(Guid));

        AssertIdentifier(
            transaction,
            nameof(Transaction.CategoryId),
            "category_id",
            typeof(Guid));

        AssertEnum(
            wallet,
            nameof(Wallet.Type),
            "type");

        AssertEnum(
            category,
            nameof(Category.Type),
            "type");

        AssertEnum(
            transaction,
            nameof(Transaction.Type),
            "type");

        AssertCurrency(
            wallet.FindProperty(nameof(Wallet.Currency)),
            StoreObjectIdentifier.Table("wallets", schema: null));

        var amountComplexProperty = transaction.FindComplexProperty(
            nameof(Transaction.Amount));

        Assert.NotNull(amountComplexProperty);

        var amount = amountComplexProperty.ComplexType.FindProperty(
            nameof(Money.Amount));

        Assert.NotNull(amount);
        Assert.Equal(
            "amount",
            amount.GetColumnName(
                StoreObjectIdentifier.Table(
                    "transactions",
                    schema: null)));
        Assert.Equal("numeric(19,4)", amount.GetColumnType());
        Assert.Equal(Money.Precision, amount.GetPrecision());
        Assert.Equal(Money.Scale, amount.GetScale());
        Assert.False(amount.IsNullable);

        var amountCurrency = amountComplexProperty.ComplexType.FindProperty(
            nameof(Money.Currency));

        AssertCurrency(
            amountCurrency,
            StoreObjectIdentifier.Table(
                "transactions",
                schema: null));

        AssertTimestamp(
            wallet,
            nameof(Wallet.CreatedAt),
            "created_at");

        AssertTimestamp(
            category,
            nameof(Category.CreatedAt),
            "created_at");

        AssertTimestamp(
            transaction,
            nameof(Transaction.OccurredAt),
            "occurred_at");

        AssertTimestamp(
            transaction,
            nameof(Transaction.CreatedAt),
            "created_at");
    }

    [Fact]
    public void Model_ShouldUseExplicitConstraintAndIndexNames()
    {
        using var context = CreateContext();

        var model = GetDesignTimeModel(context);

        var wallet = GetEntityType<Wallet>(model);
        var category = GetEntityType<Category>(model);
        var transaction = GetEntityType<Transaction>(model);

        Assert.Equal("pk_wallets", wallet.FindPrimaryKey()?.GetName());
        Assert.Equal("pk_categories", category.FindPrimaryKey()?.GetName());
        Assert.Equal(
            "pk_transactions",
            transaction.FindPrimaryKey()?.GetName());

        AssertCheckConstraint(
            wallet,
            "ck_wallets_currency_code_format",
            "currency_code ~ '^[A-Z]{3}$'");

        AssertCheckConstraint(
            wallet,
            "ck_wallets_type_defined",
            "type IN (1, 2, 3, 4, 5, 6, 7)");

        AssertCheckConstraint(
            category,
            "ck_categories_type_defined",
            "type IN (1, 2)");

        AssertCheckConstraint(
            transaction,
            "ck_transactions_amount_positive",
            "amount > 0");

        AssertCheckConstraint(
            transaction,
            "ck_transactions_currency_code_format",
            "currency_code ~ '^[A-Z]{3}$'");

        AssertCheckConstraint(
            transaction,
            "ck_transactions_type_defined",
            "type IN (1, 2, 3)");

        var indexNames = transaction
            .GetIndexes()
            .Select(index => index.GetDatabaseName())
            .ToArray();

        Assert.Contains("ix_transactions_wallet_id", indexNames);
        Assert.Contains("ix_transactions_category_id", indexNames);
    }

    [Fact]
    public void TransactionForeignKeys_ShouldRestrictPrincipalDeletion()
    {
        using var context = CreateContext();

        var model = GetDesignTimeModel(context);
        var transaction = GetEntityType<Transaction>(model);

        var walletForeignKey = transaction
            .GetForeignKeys()
            .Single(foreignKey =>
                foreignKey.PrincipalEntityType.ClrType == typeof(Wallet));

        var categoryForeignKey = transaction
            .GetForeignKeys()
            .Single(foreignKey =>
                foreignKey.PrincipalEntityType.ClrType == typeof(Category));

        Assert.Equal(
            DeleteBehavior.Restrict,
            walletForeignKey.DeleteBehavior);
        Assert.Equal(
            "fk_transactions_wallets_wallet_id",
            walletForeignKey.GetConstraintName());

        Assert.Equal(
            DeleteBehavior.Restrict,
            categoryForeignKey.DeleteBehavior);
        Assert.Equal(
            "fk_transactions_categories_category_id",
            categoryForeignKey.GetConstraintName());
    }

    private static IModel GetDesignTimeModel(
        EfCoreCompatibilityDbContext context)
    {
        return context.GetService<IDesignTimeModel>().Model;
    }

    private static EfCoreCompatibilityDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<EfCoreCompatibilityDbContext>()
                .UseNpgsql(
                    "Host=localhost;Port=5432;" +
                    "Database=spendly_model;" +
                    "Username=spendly;Password=spendly")
                .Options;

        return new EfCoreCompatibilityDbContext(options);
    }

    private static IEntityType GetEntityType<TEntity>(IModel model)
    {
        var entityType = model.FindEntityType(typeof(TEntity));

        Assert.NotNull(entityType);

        return entityType;
    }

    private static void AssertIdentifier(
        IEntityType entityType,
        string propertyName,
        string columnName,
        Type providerClrType)
    {
        var property = entityType.FindProperty(propertyName);

        Assert.NotNull(property);

        var table = StoreObjectIdentifier.Table(
            entityType.GetTableName()!,
            entityType.GetSchema());

        Assert.Equal(columnName, property.GetColumnName(table));
        Assert.Equal("uuid", property.GetColumnType());
        Assert.Equal(
            providerClrType,
            property.GetValueConverter()?.ProviderClrType);
        Assert.Equal(ValueGenerated.Never, property.ValueGenerated);
    }

    private static void AssertEnum(
        IEntityType entityType,
        string propertyName,
        string columnName)
    {
        var property = entityType.FindProperty(propertyName);

        Assert.NotNull(property);

        var table = StoreObjectIdentifier.Table(
            entityType.GetTableName()!,
            entityType.GetSchema());

        Assert.Equal(columnName, property.GetColumnName(table));
        Assert.Equal("smallint", property.GetColumnType());
        Assert.Equal(
            typeof(short),
            property.GetValueConverter()?.ProviderClrType);
        Assert.False(property.IsNullable);
    }

    private static void AssertCurrency(
        IProperty? property,
        StoreObjectIdentifier table)
    {
        Assert.NotNull(property);
        Assert.Equal(
            "currency_code",
            property.GetColumnName(table));
        Assert.Equal(
            "character varying(3)",
            property.GetColumnType());
        Assert.Equal(
            Currency.CodeLength,
            property.GetMaxLength());
        Assert.Equal(
            typeof(string),
            property.GetValueConverter()?.ProviderClrType);
        Assert.False(property.IsNullable);
    }

    private static void AssertTimestamp(
        IEntityType entityType,
        string propertyName,
        string columnName)
    {
        var property = entityType.FindProperty(propertyName);

        Assert.NotNull(property);

        var table = StoreObjectIdentifier.Table(
            entityType.GetTableName()!,
            entityType.GetSchema());

        Assert.Equal(columnName, property.GetColumnName(table));
        Assert.Equal(
            "timestamp with time zone",
            property.GetColumnType());
        Assert.False(property.IsNullable);
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
}
