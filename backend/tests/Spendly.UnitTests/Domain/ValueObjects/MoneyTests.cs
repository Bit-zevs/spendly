using System.Globalization;
using System.Reflection;
using Spendly.Domain.Errors;
using Spendly.Domain.ValueObjects;
using Spendly.UnitTests.TestUtilities;

namespace Spendly.UnitTests.Domain.ValueObjects;

public sealed class MoneyTests
{
    public static TheoryData<decimal> NegativeAmounts => new()
    {
        -0.01m,
        -1m,
        -999_999.99m
    };

    public static TheoryData<decimal> NotPositiveAmounts => new()
    {
        0m,
        -0.01m,
        -10m
    };

    [Fact]
    public void From_ShouldCreateMoney_WhenAmountIsNonNegativeAndCurrencyIsProvided()
    {
        var money = Money.From(1200.50m, Currency.Usd);

        Assert.Equal(1200.50m, money.Amount);
        Assert.Equal(Currency.Usd, money.Currency);
        Assert.True(money.IsPositive);
        Assert.False(money.IsZero);
    }

    [Fact]
    public void From_ShouldCreateZeroMoney_WhenAmountIsZero()
    {
        var money = Money.From(0m, Currency.Eur);

        Assert.Equal(0m, money.Amount);
        Assert.Equal(Currency.Eur, money.Currency);
        Assert.True(money.IsZero);
        Assert.False(money.IsPositive);
    }

    [Fact]
    public void Zero_ShouldCreateZeroMoneyInProvidedCurrency()
    {
        var money = Money.Zero(Currency.Rub);

        Assert.Equal(0m, money.Amount);
        Assert.Equal(Currency.Rub, money.Currency);
        Assert.True(money.IsZero);
        Assert.False(money.IsPositive);
    }

    [Fact]
    public void Positive_ShouldCreateMoney_WhenAmountIsGreaterThanZero()
    {
        var money = Money.Positive(10.25m, Currency.Usd);

        Assert.Equal(10.25m, money.Amount);
        Assert.Equal(Currency.Usd, money.Currency);
        Assert.True(money.IsPositive);
        Assert.False(money.IsZero);
    }

    [Theory]
    [MemberData(nameof(NegativeAmounts))]
    public void From_ShouldThrowDomainException_WhenAmountIsNegative(decimal amount)
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Money.AmountIsNegative,
            () => Money.From(amount, Currency.Usd));
    }

    [Fact]
    public void From_ShouldThrowDomainException_WhenCurrencyIsNull()
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Money.CurrencyIsRequired,
            () => Money.From(10m, null));
    }

    [Fact]
    public void Zero_ShouldThrowDomainException_WhenCurrencyIsNull()
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Money.CurrencyIsRequired,
            () => Money.Zero(null));
    }

    [Fact]
    public void Positive_ShouldThrowDomainException_WhenCurrencyIsNull()
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Money.CurrencyIsRequired,
            () => Money.Positive(10m, null));
    }

    [Theory]
    [MemberData(nameof(NotPositiveAmounts))]
    public void Positive_ShouldThrowDomainException_WhenAmountIsNotPositive(decimal amount)
    {
        var expectedError = amount < 0m
            ? DomainErrors.Money.AmountIsNegative
            : DomainErrors.Money.AmountMustBePositive;

        DomainExceptionAssert.Throws(
            expectedError,
            () => Money.Positive(amount, Currency.Usd));
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenAmountAndCurrencyAreEqual()
    {
        var first = Money.From(100.00m, Currency.Usd);
        var second = Money.From(100m, Currency.From("usd"));

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenAmountsAreDifferent()
    {
        var first = Money.From(100m, Currency.Usd);
        var second = Money.From(200m, Currency.Usd);

        Assert.NotEqual(first, second);
        Assert.False(first == second);
        Assert.True(first != second);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenCurrenciesAreDifferent()
    {
        var first = Money.From(100m, Currency.Usd);
        var second = Money.From(100m, Currency.Eur);

        Assert.NotEqual(first, second);
        Assert.False(first == second);
        Assert.True(first != second);
    }

    [Fact]
    public void HasSameCurrencyAs_ShouldReturnTrue_WhenCurrenciesAreEqual()
    {
        var first = Money.From(100m, Currency.Usd);
        var second = Money.From(50m, Currency.From("usd"));

        Assert.True(first.HasSameCurrencyAs(second));
    }

    [Fact]
    public void HasSameCurrencyAs_ShouldReturnFalse_WhenCurrenciesAreDifferent()
    {
        var first = Money.From(100m, Currency.Usd);
        var second = Money.From(50m, Currency.Eur);

        Assert.False(first.HasSameCurrencyAs(second));
    }

    [Fact]
    public void HasSameCurrencyAs_ShouldThrowArgumentNullException_WhenOtherIsNull()
    {
        var money = Money.From(100m, Currency.Usd);

        var exception = Assert.Throws<ArgumentNullException>(
            () => money.HasSameCurrencyAs(null!));

        Assert.Equal("other", exception.ParamName);
    }

    [Fact]
    public void Add_ShouldReturnSum_WhenCurrenciesAreEqual()
    {
        var first = Money.From(100.25m, Currency.Usd);
        var second = Money.From(50.75m, Currency.Usd);

        var result = first.Add(second);

        Assert.Equal(151.00m, result.Amount);
        Assert.Equal(Currency.Usd, result.Currency);
    }

    [Fact]
    public void PlusOperator_ShouldReturnSum_WhenCurrenciesAreEqual()
    {
        var first = Money.From(100m, Currency.Eur);
        var second = Money.From(50m, Currency.Eur);

        var result = first + second;

        Assert.Equal(Money.From(150m, Currency.Eur), result);
    }

    [Fact]
    public void Add_ShouldThrowDomainException_WhenCurrenciesAreDifferent()
    {
        var first = Money.From(100m, Currency.Usd);
        var second = Money.From(50m, Currency.Eur);

        DomainExceptionAssert.Throws(
            DomainErrors.Money.CurrencyMismatch,
            () => first.Add(second));
    }

    [Fact]
    public void Add_ShouldThrowArgumentNullException_WhenOtherIsNull()
    {
        var money = Money.From(100m, Currency.Usd);

        var exception = Assert.Throws<ArgumentNullException>(
            () => money.Add(null!));

        Assert.Equal("other", exception.ParamName);
    }

    [Fact]
    public void Subtract_ShouldReturnDifference_WhenCurrenciesAreEqualAndResultIsNonNegative()
    {
        var first = Money.From(100.75m, Currency.Usd);
        var second = Money.From(50.25m, Currency.Usd);

        var result = first.Subtract(second);

        Assert.Equal(50.50m, result.Amount);
        Assert.Equal(Currency.Usd, result.Currency);
    }

    [Fact]
    public void MinusOperator_ShouldReturnDifference_WhenCurrenciesAreEqual()
    {
        var first = Money.From(100m, Currency.Eur);
        var second = Money.From(40m, Currency.Eur);

        var result = first - second;

        Assert.Equal(Money.From(60m, Currency.Eur), result);
    }

    [Fact]
    public void Subtract_ShouldReturnZero_WhenAmountsAreEqual()
    {
        var first = Money.From(100m, Currency.Usd);
        var second = Money.From(100m, Currency.Usd);

        var result = first.Subtract(second);

        Assert.Equal(Money.Zero(Currency.Usd), result);
        Assert.True(result.IsZero);
        Assert.False(result.IsPositive);
    }

    [Fact]
    public void Subtract_ShouldThrowDomainException_WhenCurrenciesAreDifferent()
    {
        var first = Money.From(100m, Currency.Usd);
        var second = Money.From(50m, Currency.Eur);

        DomainExceptionAssert.Throws(
            DomainErrors.Money.CurrencyMismatch,
            () => first.Subtract(second));
    }

    [Fact]
    public void Subtract_ShouldThrowDomainException_WhenResultWouldBeNegative()
    {
        var first = Money.From(50m, Currency.Usd);
        var second = Money.From(100m, Currency.Usd);

        DomainExceptionAssert.Throws(
            DomainErrors.Money.AmountIsNegative,
            () => first.Subtract(second));
    }

    [Fact]
    public void Subtract_ShouldThrowArgumentNullException_WhenOtherIsNull()
    {
        var money = Money.From(100m, Currency.Usd);

        var exception = Assert.Throws<ArgumentNullException>(
            () => money.Subtract(null!));

        Assert.Equal("other", exception.ParamName);
    }

    [Fact]
    public void ArithmeticOperators_ShouldThrowDomainException_WhenCurrenciesAreDifferent()
    {
        var first = Money.From(100m, Currency.Usd);
        var second = Money.From(50m, Currency.Eur);

        DomainExceptionAssert.Throws(
            DomainErrors.Money.CurrencyMismatch,
            () => _ = first + second);

        DomainExceptionAssert.Throws(
            DomainErrors.Money.CurrencyMismatch,
            () => _ = first - second);
    }

    [Fact]
    public void PlusOperator_ShouldThrowArgumentNullException_WhenLeftOperandIsNull()
    {
        Money left = null!;
        var right = Money.From(100m, Currency.Usd);

        var exception = Assert.Throws<ArgumentNullException>(
            () => _ = left + right);

        Assert.Equal("left", exception.ParamName);
    }

    [Fact]
    public void MinusOperator_ShouldThrowArgumentNullException_WhenLeftOperandIsNull()
    {
        Money left = null!;
        var right = Money.From(100m, Currency.Usd);

        var exception = Assert.Throws<ArgumentNullException>(
            () => _ = left - right);

        Assert.Equal("left", exception.ParamName);
    }

    [Fact]
    public void CompareTo_ShouldReturnZero_WhenAmountsAndCurrenciesAreEqual()
    {
        var first = Money.From(100m, Currency.Usd);
        var second = Money.From(100.00m, Currency.From("usd"));

        Assert.Equal(0, first.CompareTo(second));
        Assert.True(first >= second);
        Assert.True(first <= second);
    }

    [Fact]
    public void CompareTo_ShouldReturnPositiveValue_WhenLeftAmountIsGreater()
    {
        var first = Money.From(150m, Currency.Usd);
        var second = Money.From(100m, Currency.Usd);

        Assert.True(first.CompareTo(second) > 0);
        Assert.True(first > second);
        Assert.True(first >= second);
        Assert.False(first < second);
        Assert.False(first <= second);
    }

    [Fact]
    public void CompareTo_ShouldReturnNegativeValue_WhenLeftAmountIsSmaller()
    {
        var first = Money.From(50m, Currency.Usd);
        var second = Money.From(100m, Currency.Usd);

        Assert.True(first.CompareTo(second) < 0);
        Assert.True(first < second);
        Assert.True(first <= second);
        Assert.False(first > second);
        Assert.False(first >= second);
    }

    [Fact]
    public void CompareTo_ShouldThrowDomainException_WhenCurrenciesAreDifferent()
    {
        var first = Money.From(100m, Currency.Usd);
        var second = Money.From(100m, Currency.Eur);

        DomainExceptionAssert.Throws(
            DomainErrors.Money.CurrencyMismatch,
            () => first.CompareTo(second));
    }

    [Fact]
    public void CompareTo_ShouldThrowArgumentNullException_WhenOtherIsNull()
    {
        var money = Money.From(100m, Currency.Usd);

        var exception = Assert.Throws<ArgumentNullException>(
            () => money.CompareTo(null));

        Assert.Equal("other", exception.ParamName);
    }

    [Fact]
    public void RelationalOperators_ShouldThrowDomainException_WhenCurrenciesAreDifferent()
    {
        var first = Money.From(100m, Currency.Usd);
        var second = Money.From(100m, Currency.Eur);

        DomainExceptionAssert.Throws(
            DomainErrors.Money.CurrencyMismatch,
            () => _ = first > second);

        DomainExceptionAssert.Throws(
            DomainErrors.Money.CurrencyMismatch,
            () => _ = first < second);

        DomainExceptionAssert.Throws(
            DomainErrors.Money.CurrencyMismatch,
            () => _ = first >= second);

        DomainExceptionAssert.Throws(
            DomainErrors.Money.CurrencyMismatch,
            () => _ = first <= second);
    }

    [Fact]
    public void RelationalOperators_ShouldThrowArgumentNullException_WhenLeftOperandIsNull()
    {
        Money left = null!;
        var right = Money.From(100m, Currency.Usd);

        var greaterThanException = Assert.Throws<ArgumentNullException>(
            () => _ = left > right);

        var lessThanException = Assert.Throws<ArgumentNullException>(
            () => _ = left < right);

        var greaterThanOrEqualException = Assert.Throws<ArgumentNullException>(
            () => _ = left >= right);

        var lessThanOrEqualException = Assert.Throws<ArgumentNullException>(
            () => _ = left <= right);

        Assert.Equal("left", greaterThanException.ParamName);
        Assert.Equal("left", lessThanException.ParamName);
        Assert.Equal("left", greaterThanOrEqualException.ParamName);
        Assert.Equal("left", lessThanOrEqualException.ParamName);
    }

    [Fact]
    public void DecimalArithmetic_ShouldNotHaveDoubleOrFloatRoundingIssue()
    {
        var first = Money.From(0.1m, Currency.Usd);
        var second = Money.From(0.2m, Currency.Usd);

        var result = first + second;

        Assert.Equal(0.3m, result.Amount);
        Assert.Equal(Currency.Usd, result.Currency);
    }

    [Fact]
    public void ToString_ShouldReturnInvariantAmountAndCurrencyCode()
    {
        var money = Money.From(1200.50m, Currency.Usd);

        Assert.Equal("1200.50 USD", money.ToString());
    }

    [Fact]
    public void ToString_ShouldUseProvidedAmountFormatAndFormatProvider()
    {
        var money = Money.From(1200.5m, Currency.Eur);
        var formatProvider = new NumberFormatInfo
        {
            NumberDecimalSeparator = ","
        };

        var result = money.ToString("0.00", formatProvider);

        Assert.Equal("1200,50 EUR", result);
    }

    [Fact]
    public void PublicApi_ShouldExposeDecimalAmount()
    {
        var amountProperty = typeof(Money).GetProperty(nameof(Money.Amount));

        Assert.NotNull(amountProperty);
        Assert.Equal(typeof(decimal), amountProperty.PropertyType);
    }

    [Fact]
    public void PublicFactories_ShouldAcceptDecimalAmount()
    {
        var fromMethod = typeof(Money).GetMethod(
            nameof(Money.From),
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(decimal), typeof(Currency)],
            modifiers: null);

        var positiveMethod = typeof(Money).GetMethod(
            nameof(Money.Positive),
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(decimal), typeof(Currency)],
            modifiers: null);

        Assert.NotNull(fromMethod);
        Assert.NotNull(positiveMethod);
    }

    [Fact]
    public void PublicApi_ShouldNotExposeDoubleOrFloat()
    {
        var forbiddenUsages = typeof(Money)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .SelectMany(GetMemberTypes)
            .Where(IsDoubleOrFloat)
            .ToArray();

        Assert.Empty(forbiddenUsages);
    }

    private static IEnumerable<Type> GetMemberTypes(MemberInfo member)
    {
        return member switch
        {
            PropertyInfo property => [property.PropertyType],
            FieldInfo field => [field.FieldType],
            ConstructorInfo constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType),
            MethodInfo method => method.GetParameters()
                .Select(parameter => parameter.ParameterType)
                .Append(method.ReturnType),
            _ => []
        };
    }

    private static bool IsDoubleOrFloat(Type type)
    {
        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        return actualType == typeof(double) || actualType == typeof(float);
    }
}
