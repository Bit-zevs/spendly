using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Spendly.Domain.ValueObjects;
using Spendly.Infrastructure.Persistence.Configuration;
using Spendly.Infrastructure.Persistence.Converters;

namespace Spendly.IntegrationTests.Persistence;

public sealed class CurrencyConverterTests
{
    [Fact]
    public void Converter_ShouldWriteCanonicalCurrencyCode()
    {
        var converter = new CurrencyConverter();
        var currency = Currency.From(" kzt ");

        var convertToProvider =
            converter.ConvertToProviderExpression.Compile();

        var providerValue = convertToProvider(currency);

        Assert.Equal("KZT", providerValue);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("RUB")]
    [InlineData("KZT")]
    public void Converter_ShouldRoundTripCanonicalCurrencyCode(string code)
    {
        var converter = new CurrencyConverter();
        var expectedCurrency = Currency.From(code);

        var convertToProvider =
            converter.ConvertToProviderExpression.Compile();
        var convertFromProvider =
            converter.ConvertFromProviderExpression.Compile();

        var providerValue = convertToProvider(expectedCurrency);
        var actualCurrency = convertFromProvider(providerValue);

        Assert.Equal(code, providerValue);
        Assert.Equal(expectedCurrency, actualCurrency);
    }

    [Fact]
    public void Converter_ShouldRestoreKnownCurrencyThroughDomainFactory()
    {
        var converter = new CurrencyConverter();
        var convertFromProvider =
            converter.ConvertFromProviderExpression.Compile();

        var currency = convertFromProvider("USD");

        Assert.Same(Currency.Usd, currency);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("usd")]
    [InlineData("Usd")]
    [InlineData(" USD ")]
    [InlineData("U1D")]
    [InlineData("РУБ")]
    public void Converter_ShouldRejectNonCanonicalProviderValue(string code)
    {
        var converter = new CurrencyConverter();
        var convertFromProvider =
            converter.ConvertFromProviderExpression.Compile();

        var exception = Assert.Throws<InvalidOperationException>(
            () => convertFromProvider(code));

        Assert.Contains("Persisted currency code", exception.Message);
    }

    [Fact]
    public void Mapping_ShouldUseApprovedPostgreSqlContract()
    {
        using var context = CreateContext();

        var model = context.GetService<IDesignTimeModel>().Model;
        var entityType = model.FindEntityType(typeof(CurrencyProbe));

        Assert.NotNull(entityType);

        var property = entityType.FindProperty(
            nameof(CurrencyProbe.Currency));

        Assert.NotNull(property);

        var table = StoreObjectIdentifier.Table(
            entityType.GetTableName()!,
            entityType.GetSchema());

        Assert.Equal(
            "currency_code",
            property.GetColumnName(table));
        Assert.Equal(
            "character varying(3)",
            property.GetColumnType());
        Assert.Equal(Currency.CodeLength, property.GetMaxLength());
        Assert.Equal(
            typeof(string),
            property.GetValueConverter()?.ProviderClrType);
        Assert.False(property.IsNullable);

        var checkConstraint = entityType
            .GetCheckConstraints()
            .Single(constraint =>
                constraint.Name ==
                "ck_currency_probes_currency_code_format");

        Assert.Equal(
            "currency_code ~ '^[A-Z]{3}$'",
            checkConstraint.Sql);
    }

    [Fact]
    public void Query_ShouldTranslateCurrencyParameter()
    {
        using var context = CreateContext();

        var currency = Currency.From("KZT");

        var sql = context
            .Set<CurrencyProbe>()
            .Where(probe => probe.Currency == currency)
            .ToQueryString();

        Assert.NotEmpty(sql);
        Assert.Contains("currency_code", sql);
    }

    [Fact]
    public void ChangeTracker_ShouldTreatEquivalentCurrenciesAsUnchanged()
    {
        using var context = CreateContext();

        var entity = new CurrencyProbe
        {
            Id = 1,
            Currency = Currency.From("KZT")
        };

        context.Attach(entity);

        entity.Currency = Currency.From(" kzt ");
        context.ChangeTracker.DetectChanges();

        var entry = context.Entry(entity);
        var currencyProperty = entry.Property(
            probe => probe.Currency);

        Assert.Equal(EntityState.Unchanged, entry.State);
        Assert.False(currencyProperty.IsModified);
    }

    private static CurrencyConverterTestDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<CurrencyConverterTestDbContext>()
                .UseNpgsql(
                    "Host=localhost;Database=spendly_converter_tests;" +
                    "Username=spendly;Password=spendly")
                .Options;

        return new CurrencyConverterTestDbContext(options);
    }

    private sealed class CurrencyConverterTestDbContext(
        DbContextOptions<CurrencyConverterTestDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CurrencyProbe>(builder =>
            {
                builder.ToTable(
                    "currency_probes",
                    tableBuilder =>
                        tableBuilder.HasCurrencyCodeCheckConstraint(
                            "ck_currency_probes_currency_code_format",
                            "currency_code"));

                builder.HasKey(probe => probe.Id);

                builder
                    .Property(probe => probe.Currency)
                    .HasCurrencyCodeMapping("currency_code");
            });
        }
    }

    private sealed class CurrencyProbe
    {
        public int Id { get; set; }

        public Currency Currency { get; set; } = Currency.Usd;
    }
}
