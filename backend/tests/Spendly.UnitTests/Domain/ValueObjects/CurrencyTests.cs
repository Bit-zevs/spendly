using Spendly.Domain.Errors;
using Spendly.Domain.ValueObjects;

namespace Spendly.UnitTests.Domain.ValueObjects;

public sealed class CurrencyTests
{
    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("RUB")]
    [InlineData("KZT")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    public void From_ShouldCreateCurrency_WhenCodeHasValidFormat(string code)
    {
        var currency = Currency.From(code);

        Assert.Equal(code, currency.Code);
    }

    [Theory]
    [InlineData("usd", "USD")]
    [InlineData("Usd", "USD")]
    [InlineData(" usd ", "USD")]
    [InlineData("eur", "EUR")]
    [InlineData(" rub ", "RUB")]
    public void From_ShouldNormalizeCurrencyCode(string code, string expectedCode)
    {
        var currency = Currency.From(code);

        Assert.Equal(expectedCode, currency.Code);
    }

    [Fact]
    public void StaticCurrencies_ShouldExposeBaseCurrencies()
    {
        Assert.Equal("USD", Currency.Usd.Code);
        Assert.Equal("EUR", Currency.Eur.Code);
        Assert.Equal("RUB", Currency.Rub.Code);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenCodesAreEqualAfterNormalization()
    {
        var first = Currency.From("usd");
        var second = Currency.From(" USD ");

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenCodesAreDifferent()
    {
        var first = Currency.From("USD");
        var second = Currency.From("EUR");

        Assert.NotEqual(first, second);
        Assert.False(first == second);
        Assert.True(first != second);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void From_ShouldThrowDomainException_WhenCodeIsRequired(string? code)
    {
        var exception = Assert.Throws<DomainException>(() => Currency.From(code));

        Assert.Equal(DomainErrors.Currency.CodeIsRequired, exception.Error);
        Assert.Equal(DomainErrors.Currency.CodeIsRequired.Code, exception.Code);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("U1D")]
    [InlineData("U-D")]
    [InlineData("12$")]
    [InlineData("РУБ")]
    [InlineData("EU€")]
    public void From_ShouldThrowDomainException_WhenCodeHasInvalidFormat(string code)
    {
        var exception = Assert.Throws<DomainException>(() => Currency.From(code));

        Assert.Equal(DomainErrors.Currency.CodeHasInvalidFormat, exception.Error);
        Assert.Equal(DomainErrors.Currency.CodeHasInvalidFormat.Code, exception.Code);
    }

    [Fact]
    public void ToString_ShouldReturnCurrencyCode()
    {
        var currency = Currency.From("usd");

        Assert.Equal("USD", currency.ToString());
    }
}
