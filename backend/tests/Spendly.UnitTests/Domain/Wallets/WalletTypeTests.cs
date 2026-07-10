using Spendly.Domain.Wallets;

namespace Spendly.UnitTests.Domain.Wallets;

public sealed class WalletTypeTests
{
    public static TheoryData<WalletType, int> StableValues => new()
    {
        { WalletType.Cash, 1 },
        { WalletType.DebitCard, 2 },
        { WalletType.CreditCard, 3 },
        { WalletType.BankAccount, 4 },
        { WalletType.Savings, 5 },
        { WalletType.Investment, 6 },
        { WalletType.Other, 7 }
    };

    [Theory]
    [MemberData(nameof(StableValues))]
    public void Values_ShouldHaveStableNumericRepresentations(
        WalletType walletType,
        int expectedValue)
    {
        Assert.Equal(expectedValue, (int)walletType);
    }

    [Fact]
    public void Enum_ShouldContainExpectedValuesOnly()
    {
        WalletType[] expectedValues =
        [
            WalletType.Cash,
            WalletType.DebitCard,
            WalletType.CreditCard,
            WalletType.BankAccount,
            WalletType.Savings,
            WalletType.Investment,
            WalletType.Other
        ];

        var actualValues = Enum.GetValues<WalletType>();

        Assert.Equal(expectedValues, actualValues);
    }

    [Fact]
    public void DefaultValue_ShouldNotRepresentValidWalletType()
    {
        var defaultValue = default(WalletType);

        Assert.False(Enum.IsDefined(defaultValue));
    }
}
