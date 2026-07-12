using System.Globalization;
using Spendly.Domain.Common;
using Spendly.Domain.Errors;

namespace Spendly.Domain.ValueObjects;

public sealed class Money : ValueObject, IComparable<Money>, IFormattable
{
    private decimal _amount;

    private Currency _currency = null!;

    /// <summary>
    /// Initializes an empty instance for persistence materialization.
    /// </summary>
    private Money()
    {
    }

    private Money(decimal amount, Currency currency)
    {
        _amount = amount;
        _currency = currency;
    }

    public decimal Amount => _amount;

    public Currency Currency => _currency;

    public bool IsZero => Amount == decimal.Zero;

    public bool IsPositive => Amount > decimal.Zero;

    public static Money From(decimal amount, Currency? currency)
    {
        if (currency is null)
        {
            throw new DomainException(DomainErrors.Money.CurrencyIsRequired);
        }

        if (amount < decimal.Zero)
        {
            throw new DomainException(DomainErrors.Money.AmountIsNegative);
        }

        return new Money(amount, currency);
    }

    public static Money Positive(decimal amount, Currency? currency)
    {
        var money = From(amount, currency);

        if (!money.IsPositive)
        {
            throw new DomainException(DomainErrors.Money.AmountMustBePositive);
        }

        return money;
    }

    public static Money Zero(Currency? currency)
    {
        return From(decimal.Zero, currency);
    }

    public Money Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        EnsureSameCurrency(other);

        return From(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        EnsureSameCurrency(other);

        return From(Amount - other.Amount, Currency);
    }

    public bool HasSameCurrencyAs(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Currency == other.Currency;
    }

    public int CompareTo(Money? other)
    {
        ArgumentNullException.ThrowIfNull(other);

        EnsureSameCurrency(other);

        return Amount.CompareTo(other.Amount);
    }

    public static Money operator +(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);

        return left.Add(right);
    }

    public static Money operator -(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);

        return left.Subtract(right);
    }

    public static bool operator >(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);

        return left.CompareTo(right) > 0;
    }

    public static bool operator <(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);

        return left.CompareTo(right) < 0;
    }

    public static bool operator >=(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);

        return left.CompareTo(right) >= 0;
    }

    public static bool operator <=(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);

        return left.CompareTo(right) <= 0;
    }

    public override string ToString()
    {
        return ToString(format: null, CultureInfo.InvariantCulture);
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var provider = formatProvider ?? CultureInfo.InvariantCulture;

        var formattedAmount = format is null
            ? Amount.ToString(provider)
            : Amount.ToString(format, provider);

        return formattedAmount + " " + Currency.Code;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    private void EnsureSameCurrency(Money other)
    {
        if (!HasSameCurrencyAs(other))
        {
            throw new DomainException(DomainErrors.Money.CurrencyMismatch);
        }
    }
}
