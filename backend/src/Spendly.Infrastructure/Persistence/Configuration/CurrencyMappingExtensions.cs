using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Spendly.Domain.ValueObjects;
using Spendly.Infrastructure.Persistence.Converters;

namespace Spendly.Infrastructure.Persistence.Configuration;

internal static class CurrencyMappingExtensions
{
    public static PropertyBuilder<Currency> HasCurrencyCodeMapping(
        this PropertyBuilder<Currency> propertyBuilder,
        string columnName)
    {
        ArgumentNullException.ThrowIfNull(propertyBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);

        return propertyBuilder
            .HasConversion(new CurrencyConverter())
            .HasColumnName(columnName)
            .HasColumnType(GetPostgreSqlColumnType())
            .HasMaxLength(Currency.CodeLength)
            .IsRequired();
    }

    public static ComplexTypePropertyBuilder<Currency> HasCurrencyCodeMapping(
        this ComplexTypePropertyBuilder<Currency> propertyBuilder,
        string columnName)
    {
        ArgumentNullException.ThrowIfNull(propertyBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);

        return propertyBuilder
            .HasConversion(new CurrencyConverter())
            .HasColumnName(columnName)
            .HasColumnType(GetPostgreSqlColumnType())
            .HasMaxLength(Currency.CodeLength)
            .IsRequired();
    }

    public static CheckConstraintBuilder HasCurrencyCodeCheckConstraint<TEntity>(
        this TableBuilder<TEntity> tableBuilder,
        string constraintName,
        string columnName)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(tableBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(constraintName);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);

        return tableBuilder.HasCheckConstraint(
            constraintName,
            $"{columnName} ~ '^[A-Z]{{{Currency.CodeLength}}}$'");
    }

    private static string GetPostgreSqlColumnType()
    {
        return $"character varying({Currency.CodeLength})";
    }
}
