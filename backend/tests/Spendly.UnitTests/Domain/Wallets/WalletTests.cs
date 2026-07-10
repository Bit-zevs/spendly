using Spendly.Domain.Errors;
using Spendly.Domain.ValueObjects;
using Spendly.Domain.Wallets;
using Spendly.UnitTests.TestUtilities;

namespace Spendly.UnitTests.Domain.Wallets;

public sealed class WalletTests
{
    private const string ValidName = "Daily expenses";

    private static readonly DateTimeOffset ValidCreatedAt = new(
        2026,
        7,
        11,
        12,
        30,
        0,
        TimeSpan.Zero);

    public static TheoryData<WalletType> ValidWalletTypes { get; } = new()
    {
        WalletType.Cash,
        WalletType.DebitCard,
        WalletType.CreditCard,
        WalletType.BankAccount,
        WalletType.Savings,
        WalletType.Investment,
        WalletType.Other
    };

    public static TheoryData<string?> InvalidNames { get; } = new()
    {
        null!,
        string.Empty,
        " ",
        "   ",
        "\t",
        "\r\n"
    };

    public static TheoryData<WalletType> InvalidWalletTypes { get; } = new()
    {
        null!,
        (WalletType)(-1),
        (WalletType)8,
        (WalletType)999,
        (WalletType)int.MaxValue
    };

    [Fact]
    public void Create_ShouldCreateWallet_WhenArgumentsAreValid()
    {
        var currency = Currency.From("KZT");

        var wallet = Wallet.Create(
            ValidName,
            WalletType.DebitCard,
            currency,
            ValidCreatedAt);

        Assert.NotEqual(default(WalletId), wallet.Id);
        Assert.NotEqual(Guid.Empty, wallet.Id.Value);
        Assert.Equal(7, wallet.Id.Value.Version);

        Assert.Equal(ValidName, wallet.Name);
        Assert.Equal(WalletType.DebitCard, wallet.Type);
        Assert.Equal(currency, wallet.Currency);
        Assert.Equal(ValidCreatedAt, wallet.CreatedAt);
    }

    [Fact]
    public void Create_ShouldGenerateDifferentIds_WhenCalledForDifferentWallets()
    {
        var firstWallet = Wallet.Create(
            "Cash",
            WalletType.Cash,
            Currency.Rub,
            ValidCreatedAt);

        var secondWallet = Wallet.Create(
            "Main card",
            WalletType.DebitCard,
            Currency.Rub,
            ValidCreatedAt);

        Assert.NotEqual(firstWallet.Id, secondWallet.Id);
        Assert.NotEqual(firstWallet.Id.Value, secondWallet.Id.Value);
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        var wallet = Wallet.Create(
            "  Main debit card  ",
            WalletType.DebitCard,
            Currency.Rub,
            ValidCreatedAt);

        Assert.Equal("Main debit card", wallet.Name);
    }

    [Theory]
    [MemberData(nameof(ValidWalletTypes))]
    public void Create_ShouldAcceptType_WhenTypeIsDefined(WalletType type)
    {
        var wallet = Wallet.Create(
            ValidName,
            type,
            Currency.Usd,
            ValidCreatedAt);

        Assert.Equal(type, wallet.Type);
    }

    [Fact]
    public void Create_ShouldConvertCreatedAtToUtc()
    {
        var createdAt = new DateTimeOffset(
            2026,
            7,
            11,
            15,
            30,
            0,
            TimeSpan.FromHours(5));

        var expectedCreatedAt = new DateTimeOffset(
            2026,
            7,
            11,
            10,
            30,
            0,
            TimeSpan.Zero);

        var wallet = Wallet.Create(
            ValidName,
            WalletType.DebitCard,
            Currency.Rub,
            createdAt);

        Assert.Equal(expectedCreatedAt, wallet.CreatedAt);
        Assert.Equal(TimeSpan.Zero, wallet.CreatedAt.Offset);
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void Create_ShouldThrowDomainException_WhenNameIsNullOrWhiteSpace(
        string? name)
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Wallet.NameIsEmpty,
            () => Wallet.Create(
                name,
                WalletType.Cash,
                Currency.Rub,
                ValidCreatedAt));
    }

    [Theory]
    [MemberData(nameof(InvalidWalletTypes))]
    public void Create_ShouldThrowDomainException_WhenTypeIsInvalid(
        WalletType type)
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Wallet.TypeIsInvalid,
            () => Wallet.Create(
                ValidName,
                type,
                Currency.Rub,
                ValidCreatedAt));
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenCurrencyIsNull()
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Wallet.CurrencyIsRequired,
            () => Wallet.Create(
                ValidName,
                WalletType.Cash,
                null,
                ValidCreatedAt));
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenCreatedAtIsDefault()
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Wallet.CreatedAtIsInvalid,
            () => Wallet.Create(
                ValidName,
                WalletType.Cash,
                Currency.Rub,
                default));
    }
}
