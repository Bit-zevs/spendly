using System.Globalization;
using System.Reflection;
using Spendly.Domain.Errors;
using Spendly.Domain.ValueObjects;

namespace Spendly.UnitTests.Domain.ValueObjects;

public sealed class MoneyTests
{
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
    }

    [Fact]
    public void Positive_ShouldCreateMoney_WhenAmountIsGreaterThanZero()
    {
        var money = Money.Positive(10.25m, Currency.Usd);

        Assert.Equal(10.25m, money.Amount);
        Assert.Equal(Currency.Usd, money.Currency);
        Assert.True(money.IsPositive);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-999999.99)]
    public void From_ShouldThrowDomainException_WhenAmountIsNegative(decimal amount)
    {
        var exception = Assert.Throws<DomainException>(() => Money.From(amount, Currency.Usd));

        Assert.Equal(DomainErrors.Money.AmountIsNegative, exception.Error);
        Assert.Equal(DomainErrors.Money.AmountIsNegative.Code, exception.Code);
    }

    [Fact]
    public void From_ShouldThrowDomainException_WhenCurrencyIsNull()
    {
        var exception = Assert.Throws<DomainException>(() => Money.From(10m, null));

        Assert.Equal(DomainErrors.Money.CurrencyIsRequired, exception.Error);
        Assert.Equal(DomainErrors.Money.CurrencyIsRequired.Code, exception.Code);
    }

    [Fact]
    public void Zero_ShouldThrowDomainException_WhenCurrencyIsNull()
    {
        var exception = Assert.Throws<DomainException>(() => Money.Zero(null));

        Assert.Equal(DomainErrors.Money.CurrencyIsRequired, exception.Error);
        Assert.Equal(DomainErrors.Money.CurrencyIsRequired.Code, exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-10)]
    public void Positive_ShouldThrowDomainException_WhenAmountIsNotPositive(decimal amount)
    {
        var expectedError = amount < 0m
            ? DomainErrors.Money.AmountIsNegative
            : DomainErrors.Money.AmountMustBePositive;

        var exception = Assert.Throws<DomainException>(() => Money.Positive(amount, Currency.Usd));

        Assert.Equal(expectedError, exception.Error);
        Assert.Equal(expectedError.Code, exception.Code);
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

        var exception = Assert.Throws<DomainException>(() => first.Add(second));

        Assert.Equal(DomainErrors.Money.CurrencyMismatch, exception.Error);
        Assert.Equal(DomainErrors.Money.CurrencyMismatch.Code, exception.Code);
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
    }

    [Fact]
    public void Subtract_ShouldThrowDomainException_WhenCurrenciesAreDifferent()
    {
        var first = Money.From(100m, Currency.Usd);
        var second = Money.From(50m, Currency.Eur);

        var exception = Assert.Throws<DomainException>(() => first.Subtract(second));

        Assert.Equal(DomainErrors.Money.CurrencyMismatch, exception.Error);
        Assert.Equal(DomainErrors.Money.CurrencyMismatch.Code, exception.Code);
    }

    [Fact]
    public void Subtract_ShouldThrowDomainException_WhenResultWouldBeNegative()
    {
        var first = Money.From(50m, Currency.Usd);
        var second = Money.From(100m, Currency.Usd);

        var exception = Assert.Throws<DomainException>(() => first.Subtract(second));

        Assert.Equal(DomainErrors.Money.AmountIsNegative, exception.Error);
        Assert.Equal(DomainErrors.Money.AmountIsNegative.Code, exception.Code);
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
    }

    [Fact]
    public void CompareTo_ShouldThrowDomainException_WhenCurrenciesAreDifferent()
    {
        var first = Money.From(100m, Currency.Usd);
        var second = Money.From(100m, Currency.Eur);

        var exception = Assert.Throws<DomainException>(() => first.CompareTo(second));

        Assert.Equal(DomainErrors.Money.CurrencyMismatch, exception.Error);
        Assert.Equal(DomainErrors.Money.CurrencyMismatch.Code, exception.Code);
    }

    [Fact]
    public void RelationalOperators_ShouldThrowDomainException_WhenCurrenciesAreDifferent()
    {
        var first = Money.From(100m, Currency.Usd);
        var second = Money.From(100m, Currency.Eur);

        var exception = Assert.Throws<DomainException>(() => first > second);

        Assert.Equal(DomainErrors.Money.CurrencyMismatch, exception.Error);
        Assert.Equal(DomainErrors.Money.CurrencyMismatch.Code, exception.Code);
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

        var result = money.ToString("0.00", CultureInfo.InvariantCulture);

        Assert.Equal("1200.50 EUR", result);
    }

    [Fact]
    public void PublicApi_ShouldNotExposeDoubleOrFloat()
    {
        var moneyType = typeof(Money);

        var forbiddenUsages = moneyType
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
