using Spendly.Domain.Categories;
using Spendly.Domain.Errors;
using Spendly.Domain.ValueObjects;
using Spendly.Domain.Wallets;
using Spendly.UnitTests.TestUtilities;

namespace Spendly.UnitTests.Domain;

public sealed class DomainBoundaryTests
{
    private static readonly DateTimeOffset ValidCreatedAt = new(
        2026,
        7,
        13,
        12,
        0,
        0,
        TimeSpan.Zero);

    [Fact]
    public void WalletCreate_ShouldAcceptName_WhenTrimmedLengthEqualsMaximum()
    {
        var expectedName = new string('w', Wallet.MaxNameLength);

        var wallet = Wallet.Create(
            $"  {expectedName}  ",
            WalletType.Cash,
            Currency.Usd,
            ValidCreatedAt);

        Assert.Equal(expectedName, wallet.Name);
    }

    [Fact]
    public void WalletCreate_ShouldThrowDomainException_WhenTrimmedNameIsTooLong()
    {
        var name = new string('w', Wallet.MaxNameLength + 1);

        DomainExceptionAssert.Throws(
            DomainErrors.Wallet.NameIsTooLong,
            () => Wallet.Create(
                name,
                WalletType.Cash,
                Currency.Usd,
                ValidCreatedAt));
    }

    [Fact]
    public void CategoryCreate_ShouldAcceptName_WhenTrimmedLengthEqualsMaximum()
    {
        var expectedName = new string('c', Category.MaxNameLength);

        var category = Category.Create(
            $"  {expectedName}  ",
            CategoryType.Expense,
            ValidCreatedAt);

        Assert.Equal(expectedName, category.Name);
    }

    [Fact]
    public void CategoryCreate_ShouldThrowDomainException_WhenTrimmedNameIsTooLong()
    {
        var name = new string('c', Category.MaxNameLength + 1);

        DomainExceptionAssert.Throws(
            DomainErrors.Category.NameIsTooLong,
            () => Category.Create(
                name,
                CategoryType.Expense,
                ValidCreatedAt));
    }

    [Fact]
    public void MoneyFrom_ShouldAcceptMaximumSupportedAmount()
    {
        var money = Money.From(Money.MaxAmount, Currency.Usd);

        Assert.Equal(Money.MaxAmount, money.Amount);
    }

    [Fact]
    public void MoneyFrom_ShouldAcceptAmountWithMaximumSupportedScale()
    {
        var money = Money.From(123.4567m, Currency.Usd);

        Assert.Equal(123.4567m, money.Amount);
    }

    [Fact]
    public void MoneyFrom_ShouldThrowDomainException_WhenAmountExceedsMaximum()
    {
        var amount = Money.MaxAmount + 0.0001m;

        DomainExceptionAssert.Throws(
            DomainErrors.Money.AmountExceedsMaximum,
            () => Money.From(amount, Currency.Usd));
    }

    [Fact]
    public void MoneyFrom_ShouldThrowDomainException_WhenAmountHasTooManyFractionalDigits()
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Money.AmountHasTooManyFractionalDigits,
            () => Money.From(123.45678m, Currency.Usd));
    }

    [Fact]
    public void MoneyAdd_ShouldThrowDomainException_WhenResultExceedsMaximum()
    {
        var left = Money.From(Money.MaxAmount, Currency.Usd);
        var right = Money.From(0.0001m, Currency.Usd);

        DomainExceptionAssert.Throws(
            DomainErrors.Money.AmountExceedsMaximum,
            () => left.Add(right));
    }
}
