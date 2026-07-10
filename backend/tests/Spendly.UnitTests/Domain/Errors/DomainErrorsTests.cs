using Spendly.Domain.Errors;

namespace Spendly.UnitTests.Domain.Errors;

public sealed class DomainErrorsTests
{
    [Fact]
    public void KnownErrors_ShouldHaveStableCodes()
    {
        Assert.Equal("Currency.Code.Required", DomainErrors.Currency.CodeIsRequired.Code);
        Assert.Equal("Currency.Code.InvalidFormat", DomainErrors.Currency.CodeHasInvalidFormat.Code);

        Assert.Equal("Money.Amount.Negative", DomainErrors.Money.AmountIsNegative.Code);
        Assert.Equal("Money.Amount.NotPositive", DomainErrors.Money.AmountMustBePositive.Code);
        Assert.Equal("Money.Currency.Required", DomainErrors.Money.CurrencyIsRequired.Code);
        Assert.Equal("Money.Currency.Mismatch", DomainErrors.Money.CurrencyMismatch.Code);

        Assert.Equal("Wallet.Name.Empty", DomainErrors.Wallet.NameIsEmpty.Code);
        Assert.Equal("Wallet.Type.Invalid", DomainErrors.Wallet.TypeIsInvalid.Code);
        Assert.Equal("Wallet.Currency.Required", DomainErrors.Wallet.CurrencyIsRequired.Code);
        Assert.Equal("Wallet.CreatedAt.Invalid", DomainErrors.Wallet.CreatedAtIsInvalid.Code);

        Assert.Equal("Category.Name.Empty", DomainErrors.Category.NameIsEmpty.Code);

        Assert.Equal("Transaction.Amount.NotPositive", DomainErrors.Transaction.AmountMustBePositive.Code);
        Assert.Equal("Transaction.Wallet.Required", DomainErrors.Transaction.WalletIsRequired.Code);
    }

    [Fact]
    public void KnownErrors_ShouldHaveNonEmptyMessages()
    {
        var errors = GetKnownErrors();

        foreach (var error in errors)
        {
            Assert.False(string.IsNullOrWhiteSpace(error.Message));
        }
    }

    [Fact]
    public void KnownErrors_ShouldHaveUniqueCodes()
    {
        var errors = GetKnownErrors();

        var codes = errors
            .Select(error => error.Code)
            .ToArray();

        var uniqueCodesCount = codes
            .Distinct(StringComparer.Ordinal)
            .Count();

        Assert.Equal(codes.Length, uniqueCodesCount);
    }

    private static DomainError[] GetKnownErrors()
    {
        return
        [
            DomainErrors.Currency.CodeIsRequired,
            DomainErrors.Currency.CodeHasInvalidFormat,
            DomainErrors.Money.AmountIsNegative,
            DomainErrors.Money.AmountMustBePositive,
            DomainErrors.Money.CurrencyIsRequired,
            DomainErrors.Money.CurrencyMismatch,
            DomainErrors.Wallet.NameIsEmpty,
            DomainErrors.Wallet.TypeIsInvalid,
            DomainErrors.Wallet.CurrencyIsRequired,
            DomainErrors.Wallet.CreatedAtIsInvalid,
            DomainErrors.Category.NameIsEmpty,
            DomainErrors.Transaction.AmountMustBePositive,
            DomainErrors.Transaction.WalletIsRequired
        ];
    }
}
