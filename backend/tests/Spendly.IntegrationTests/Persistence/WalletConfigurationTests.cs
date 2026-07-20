using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Spendly.Domain.ValueObjects;
using Spendly.Domain.Wallets;
using Spendly.Infrastructure.Persistence;

namespace Spendly.IntegrationTests.Persistence;

public sealed class WalletConfigurationTests
{
    [Fact]
    public void Mapping_ShouldUseApprovedPostgreSqlContract()
    {
        using var context = CreateContext();

        var model = context.GetService<IDesignTimeModel>().Model;
        var entityType = model.FindEntityType(typeof(Wallet));

        Assert.NotNull(entityType);
        Assert.Equal("wallets", entityType.GetTableName());
        Assert.Equal("pk_wallets", entityType.FindPrimaryKey()?.GetName());

        var table = StoreObjectIdentifier.Table(
            entityType.GetTableName()!,
            entityType.GetSchema());

        AssertProperty(
            entityType,
            table,
            nameof(Wallet.Id),
            "id",
            "uuid",
            typeof(Guid));

        var idProperty = entityType.FindProperty(nameof(Wallet.Id));

        Assert.NotNull(idProperty);
        Assert.Equal(ValueGenerated.Never, idProperty.ValueGenerated);

        AssertProperty(
            entityType,
            table,
            nameof(Wallet.Name),
            "name");

        var nameProperty = entityType.FindProperty(nameof(Wallet.Name));

        Assert.NotNull(nameProperty);
        Assert.Equal(Wallet.MaxNameLength, nameProperty.GetMaxLength());

        AssertProperty(
            entityType,
            table,
            nameof(Wallet.Type),
            "type",
            "smallint",
            typeof(short));

        AssertProperty(
            entityType,
            table,
            nameof(Wallet.Currency),
            "currency_code",
            $"character varying({Currency.CodeLength})",
            typeof(string));

        var currencyProperty = entityType.FindProperty(
            nameof(Wallet.Currency));

        Assert.NotNull(currencyProperty);
        Assert.Equal(
            Currency.CodeLength,
            currencyProperty.GetMaxLength());

        AssertProperty(
            entityType,
            table,
            nameof(Wallet.CreatedAt),
            "created_at",
            "timestamp with time zone");

        AssertCheckConstraint(
            entityType,
            "ck_wallets_currency_code_format",
            "currency_code ~ '^[A-Z]{3}$'");

        AssertCheckConstraint(
            entityType,
            "ck_wallets_type_defined",
            "type IN (1, 2, 3, 4, 5, 6, 7)");
    }

    [Fact]
    public void Mapping_ShouldMatchCurrentDomainShape()
    {
        using var context = CreateContext();

        var model = context.GetService<IDesignTimeModel>().Model;
        var entityType = model.FindEntityType(typeof(Wallet));

        Assert.NotNull(entityType);

        var propertyNames = entityType
            .GetProperties()
            .Select(property => property.Name)
            .Order()
            .ToArray();

        string[] expectedPropertyNames =
        [
            nameof(Wallet.CreatedAt),
            nameof(Wallet.Currency),
            nameof(Wallet.Id),
            nameof(Wallet.Name),
            nameof(Wallet.Type)
        ];

        Assert.Equal(expectedPropertyNames, propertyNames);
        Assert.Empty(entityType.GetForeignKeys());
        Assert.Empty(entityType.GetIndexes());
        Assert.Empty(entityType.GetNavigations());
    }

    private static void AssertProperty(
        IEntityType entityType,
        StoreObjectIdentifier table,
        string propertyName,
        string columnName,
        string? columnType = null,
        Type? providerClrType = null)
    {
        var property = entityType.FindProperty(propertyName);

        Assert.NotNull(property);
        Assert.Equal(columnName, property.GetColumnName(table));
        Assert.False(property.IsNullable);

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

    private static WalletConfigurationTestDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<WalletConfigurationTestDbContext>()
                .UseNpgsql(
                    "Host=localhost;Database=spendly_wallet_mapping_tests;" +
                    "Username=spendly;Password=spendly")
                .Options;

        return new WalletConfigurationTestDbContext(options);
    }

    private sealed class WalletConfigurationTestDbContext(
        DbContextOptions<WalletConfigurationTestDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(SpendlyDbContext).Assembly);
        }
    }
}
