using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Spendly.Domain.ValueObjects;

namespace Spendly.Infrastructure.Persistence.Configuration;

internal static class MoneyMappingExtensions
{
    private const string AmountBackingFieldName = "_amount";

    private const string CurrencyBackingFieldName = "_currency";

    public static ComplexPropertyBuilder<Money> HasMoneyMapping(
        this ComplexPropertyBuilder<Money> complexPropertyBuilder,
        string moneyBackingFieldName,
        string amountColumnName,
        string currencyColumnName)
    {
        ArgumentNullException.ThrowIfNull(complexPropertyBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(moneyBackingFieldName);
        ArgumentException.ThrowIfNullOrWhiteSpace(amountColumnName);
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyColumnName);

        complexPropertyBuilder
            .HasField(moneyBackingFieldName)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        complexPropertyBuilder
            .Property(money => money.Amount)
            .HasField(AmountBackingFieldName)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName(amountColumnName)
            .HasColumnType(GetPostgreSqlAmountColumnType())
            .HasPrecision(Money.Precision, Money.Scale)
            .IsRequired();

        complexPropertyBuilder
            .Property(money => money.Currency)
            .HasField(CurrencyBackingFieldName)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasCurrencyCodeMapping(currencyColumnName);

        return complexPropertyBuilder;
    }

    private static string GetPostgreSqlAmountColumnType()
    {
        return $"numeric({Money.Precision},{Money.Scale})";
    }
}
