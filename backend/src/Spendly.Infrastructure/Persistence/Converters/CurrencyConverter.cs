using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Spendly.Domain.ValueObjects;

namespace Spendly.Infrastructure.Persistence.Converters;

internal sealed class CurrencyConverter : ValueConverter<Currency, string>
{
    private static readonly ConverterMappingHints DefaultMappingHints =
        new(size: Currency.CodeLength);

    public CurrencyConverter()
        : base(
            currency => GetProviderValue(currency),
            code => GetModelValue(code),
            DefaultMappingHints)
    {
    }

    private static string GetProviderValue(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        return currency.Code;
    }

    private static Currency GetModelValue(string code)
    {
        if (!IsCanonicalCode(code))
        {
            throw new InvalidOperationException(
                "Persisted currency code must contain exactly " +
                $"{Currency.CodeLength} uppercase Latin letters.");
        }

        return Currency.From(code);
    }

    private static bool IsCanonicalCode(string? code)
    {
        if (code is null || code.Length != Currency.CodeLength)
        {
            return false;
        }

        foreach (var character in code)
        {
            if (character is < 'A' or > 'Z')
            {
                return false;
            }
        }

        return true;
    }
}
