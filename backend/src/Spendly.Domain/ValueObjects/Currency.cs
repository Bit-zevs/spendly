using Spendly.Domain.Common;
using Spendly.Domain.Errors;

namespace Spendly.Domain.ValueObjects;

public sealed class Currency : ValueObject
{
    public const int CodeLength = 3;

    public static Currency Usd { get; } = new("USD");

    public static Currency Eur { get; } = new("EUR");

    public static Currency Rub { get; } = new("RUB");

    private Currency(string code)
    {
        Code = code;
    }

    public string Code { get; }

    public static Currency From(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException(DomainErrors.Currency.CodeIsRequired);
        }

        var normalizedCode = NormalizeCode(code);

        if (!HasValidCodeFormat(normalizedCode))
        {
            throw new DomainException(DomainErrors.Currency.CodeHasInvalidFormat);
        }

        return Create(normalizedCode);
    }

    public override string ToString()
    {
        return Code;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }

    private static Currency Create(string normalizedCode)
    {
        return normalizedCode switch
        {
            "USD" => Usd,
            "EUR" => Eur,
            "RUB" => Rub,
            _ => new Currency(normalizedCode)
        };
    }

    private static string NormalizeCode(string code)
    {
        return code.Trim().ToUpperInvariant();
    }

    private static bool HasValidCodeFormat(string code)
    {
        if (code.Length is not CodeLength)
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
