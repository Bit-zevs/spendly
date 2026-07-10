using Spendly.Domain.Errors;
using Spendly.Domain.ValueObjects;
using Spendly.Domain.Wallets;
using Spendly.UnitTests.TestUtilities;

namespace Spendly.UnitTests.Domain.Wallets;

public sealed class WalletTests
{
    public static TheoryData<WalletType> ValidWalletTypes => new()
    {
        WalletType.Cash,
        WalletType.DebitCard,
        WalletType.CreditCard,
        WalletType.BankAccount,
        WalletType.Savings,
        WalletType.Investment,
        WalletType.Other
    };

    public static TheoryData<string> BlankNames => new()
    {
        string.Empty,
        " ",
        "   ",
        "\t",
        "\r\n"
    };

    public static TheoryData<WalletType> InvalidWalletTypes => new()
    {
        default(WalletType),
        (WalletType)8,
        (WalletType)999,
        (WalletType)int.MaxValue
    };

    [Fact]
    public void Create_ShouldCreateWallet_WhenArgumentsAreValid()
    {
        var currency = Currency.From("KZT");
        var createdAt = new DateTimeOffset(
            2026,
            7,
            11,
            12,
            30,
            0,
            TimeSpan.Zero);

        var wallet = Wallet.Create(
            "Daily expenses",
            WalletType.DebitCard,
            currency,
            createdAt);

        Assert.NotEqual(default(WalletId), wallet.Id);
        Assert.NotEqual(Guid.Empty, wallet.Id.Value);
        Assert.Equal(7, wallet.Id.Value.Version);

        Assert.Equal("Daily expenses", wallet.Name);
        Assert.Equal(WalletType.DebitCard, wallet.Type);
        Assert.Same(currency, wallet.Currency);
        Assert.Equal(createdAt, wallet.CreatedAt);
    }

    [Fact]
    public void Create_ShouldGenerateDifferentIds_ForDifferentWallets()
    {
        var createdAt = new DateTimeOffset(
            2026,
            7,
            11,
            12,
            30,
            0,
            TimeSpan.Zero);

        var first = Wallet.Create(
            "Cash",
            WalletType.Cash,
            Currency.Rub,
            createdAt);

        var second = Wallet.Create(
            "Main card",
            WalletType.DebitCard,
            Currency.Rub,
            createdAt);

        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Create_ShouldTrimWalletName()
    {
        var wallet = Wallet.Create(
            "  Main debit card  ",
            WalletType.DebitCard,
            Currency.Rub,
            DateTimeOffset.UtcNow);

        Assert.Equal("Main debit card", wallet.Name);
    }

    [Theory]
    [MemberData(nameof(ValidWalletTypes))]
    public void Create_ShouldAcceptEveryDefinedWalletType(WalletType type)
    {
        var wallet = Wallet.Create(
            "Wallet",
            type,
            Currency.Usd,
            DateTimeOffset.UtcNow);

        Assert.Equal(type, wallet.Type);
    }

    [Fact]
    public void Create_ShouldNormalizeCreationTimeToUtc()
    {
        var createdAt = new DateTimeOffset(
            2026,
            7,
            11,
            15,
            30,
            0,
            TimeSpan.FromHours(5));

        var wallet = Wallet.Create(
            "Main card",
            WalletType.DebitCard,
            Currency.Rub,
            createdAt);

        var expectedCreatedAt = new DateTimeOffset(
            2026,
            7,
            11,
            10,
            30,
            0,
            TimeSpan.Zero);

        Assert.Equal(expectedCreatedAt, wallet.CreatedAt);
        Assert.Equal(TimeSpan.Zero, wallet.CreatedAt.Offset);
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenNameIsNull()
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Wallet.NameIsEmpty,
            () => Wallet.Create(
                null,
                WalletType.Cash,
                Currency.Rub,
                DateTimeOffset.UtcNow));
    }

    [Theory]
    [MemberData(nameof(BlankNames))]
    public void Create_ShouldThrowDomainException_WhenNameIsBlank(string name)
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Wallet.NameIsEmpty,
            () => Wallet.Create(
                name,
                WalletType.Cash,
                Currency.Rub,
                DateTimeOffset.UtcNow));
    }

    [Theory]
    [MemberData(nameof(InvalidWalletTypes))]
    public void Create_ShouldThrowDomainException_WhenTypeIsInvalid(WalletType type)
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Wallet.TypeIsInvalid,
            () => Wallet.Create(
                "Wallet",
                type,
                Currency.Rub,
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenCurrencyIsNull()
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Wallet.CurrencyIsRequired,
            () => Wallet.Create(
                "Wallet",
                WalletType.Cash,
                null,
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenCreatedAtHasDefaultValue()
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Wallet.CreatedAtIsInvalid,
            () => Wallet.Create(
                "Wallet",
                WalletType.Cash,
                Currency.Rub,
                default));
    }
}
