using Spendly.Domain.Errors;
using Spendly.Domain.ValueObjects;
using Spendly.UnitTests.TestUtilities;

namespace Spendly.UnitTests.Domain.ValueObjects;

public sealed class CurrencyTests
{
    public static TheoryData<string> ValidCurrencyCodes => new()
    {
        "USD",
        "EUR",
        "RUB",
        "KZT",
        "GBP",
        "JPY",
        "CHF",
        "CAD"
    };

    public static TheoryData<string, string> CodesToNormalize => new()
    {
        { "usd", "USD" },
        { "Usd", "USD" },
        { "uSd", "USD" },
        { " usd ", "USD" },
        { " eur ", "EUR" },
        { " rub ", "RUB" },
        { "\tgbp\n", "GBP" }
    };

    public static TheoryData<string> BlankCurrencyCodes => new()
    {
        string.Empty,
        " ",
        "   ",
        "\t",
        "\r\n"
    };

    public static TheoryData<string> InvalidCurrencyCodes => new()
    {
        "US",
        "USDD",
        "U1D",
        "U-D",
        "12$",
        "РУБ",
        "EU€",
        "U$D",
        "USD1",
        "US D"
    };

    public static TheoryData<string, Currency> KnownCurrencyCodes => new()
    {
        { "USD", Currency.Usd },
        { "usd", Currency.Usd },
        { " USD ", Currency.Usd },
        { "EUR", Currency.Eur },
        { "eur", Currency.Eur },
        { "RUB", Currency.Rub },
        { "rub", Currency.Rub }
    };

    [Theory]
    [MemberData(nameof(ValidCurrencyCodes))]
    public void From_ShouldCreateCurrency_WhenCodeHasValidFormat(string code)
    {
        var currency = Currency.From(code);

        Assert.Equal(code, currency.Code);
    }

    [Theory]
    [MemberData(nameof(CodesToNormalize))]
    public void From_ShouldNormalizeCurrencyCode(string code, string expectedCode)
    {
        var currency = Currency.From(code);

        Assert.Equal(expectedCode, currency.Code);
    }

    [Theory]
    [MemberData(nameof(KnownCurrencyCodes))]
    public void From_ShouldReturnKnownCurrencyInstance_WhenCodeIsKnown(string code, Currency expectedCurrency)
    {
        var currency = Currency.From(code);

        Assert.Same(expectedCurrency, currency);
    }

    [Fact]
    public void StaticCurrencies_ShouldExposeBaseCurrencies()
    {
        Assert.Equal("USD", Currency.Usd.Code);
        Assert.Equal("EUR", Currency.Eur.Code);
        Assert.Equal("RUB", Currency.Rub.Code);
    }

    [Fact]
    public void CodeLength_ShouldBeThree()
    {
        Assert.Equal(3, Currency.CodeLength);
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

    [Fact]
    public void From_ShouldThrowDomainException_WhenCodeIsNull()
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Currency.CodeIsRequired,
            () => Currency.From(null));
    }

    [Theory]
    [MemberData(nameof(BlankCurrencyCodes))]
    public void From_ShouldThrowDomainException_WhenCodeIsBlank(string code)
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Currency.CodeIsRequired,
            () => Currency.From(code));
    }

    [Theory]
    [MemberData(nameof(InvalidCurrencyCodes))]
    public void From_ShouldThrowDomainException_WhenCodeHasInvalidFormat(string code)
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Currency.CodeHasInvalidFormat,
            () => Currency.From(code));
    }

    [Fact]
    public void ToString_ShouldReturnCurrencyCode()
    {
        var currency = Currency.From("usd");

        Assert.Equal("USD", currency.ToString());
    }
}
