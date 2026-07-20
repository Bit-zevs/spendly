using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Spendly.Domain.ValueObjects;
using Spendly.Infrastructure.Persistence.Configuration;

namespace Spendly.IntegrationTests.Persistence;

public sealed class MoneyMappingExtensionsTests
{
    [Fact]
    public void Mapping_ShouldUseApprovedPostgreSqlContract()
    {
        using var context = CreateContext();

        var model = context.GetService<IDesignTimeModel>().Model;
        var entityType = model.FindEntityType(typeof(MoneyProbe));

        Assert.NotNull(entityType);

        var moneyProperty = entityType.FindComplexProperty(
            nameof(MoneyProbe.Value));

        Assert.NotNull(moneyProperty);
        Assert.False(moneyProperty.IsNullable);
        Assert.Equal("_value", moneyProperty.FieldInfo?.Name);
        Assert.Equal(
            PropertyAccessMode.Field,
            moneyProperty.GetPropertyAccessMode());

        var table = StoreObjectIdentifier.Table(
            entityType.GetTableName()!,
            entityType.GetSchema());

        var amountProperty = moneyProperty.ComplexType.FindProperty(
            nameof(Money.Amount));

        Assert.NotNull(amountProperty);
        Assert.Equal("_amount", amountProperty.FieldInfo?.Name);
        Assert.Equal(
            PropertyAccessMode.Field,
            amountProperty.GetPropertyAccessMode());
        Assert.Equal(
            "amount",
            amountProperty.GetColumnName(table));
        Assert.Equal(
            $"numeric({Money.Precision},{Money.Scale})",
            amountProperty.GetColumnType());
        Assert.Equal(
            Money.Precision,
            amountProperty.GetPrecision());
        Assert.Equal(
            Money.Scale,
            amountProperty.GetScale());
        Assert.False(amountProperty.IsNullable);

        var currencyProperty = moneyProperty.ComplexType.FindProperty(
            nameof(Money.Currency));

        Assert.NotNull(currencyProperty);
        Assert.Equal("_currency", currencyProperty.FieldInfo?.Name);
        Assert.Equal(
            PropertyAccessMode.Field,
            currencyProperty.GetPropertyAccessMode());
        Assert.Equal(
            "currency_code",
            currencyProperty.GetColumnName(table));
        Assert.Equal(
            "character varying(3)",
            currencyProperty.GetColumnType());
        Assert.Equal(
            Currency.CodeLength,
            currencyProperty.GetMaxLength());
        Assert.Equal(
            typeof(string),
            currencyProperty.GetValueConverter()?.ProviderClrType);
        Assert.False(currencyProperty.IsNullable);
    }

    private static MoneyMappingTestDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<MoneyMappingTestDbContext>()
                .UseNpgsql(
                    "Host=localhost;" +
                    "Database=spendly_money_mapping_tests;" +
                    "Username=spendly;" +
                    "Password=spendly")
                .Options;

        return new MoneyMappingTestDbContext(options);
    }

    private sealed class MoneyMappingTestDbContext(
        DbContextOptions<MoneyMappingTestDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MoneyProbe>(builder =>
            {
                builder.ToTable("money_probes");

                builder.HasKey(probe => probe.Id);

                builder
                    .ComplexProperty(probe => probe.Value)
                    .HasMoneyMapping(
                        moneyBackingFieldName: "_value",
                        amountColumnName: "amount",
                        currencyColumnName: "currency_code");
            });
        }
    }

    private sealed class MoneyProbe
    {
        private Money _value = Money.Zero(Currency.Usd);

        public int Id { get; set; }

        public Money Value => _value;
    }
}
