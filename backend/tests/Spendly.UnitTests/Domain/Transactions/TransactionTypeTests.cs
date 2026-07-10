using Spendly.Domain.Transactions;

namespace Spendly.UnitTests.Domain.Transactions;

public sealed class TransactionTypeTests
{
    public static TheoryData<TransactionType, int> StableValues => new()
    {
        { TransactionType.Income, 1 },
        { TransactionType.Expense, 2 },
        { TransactionType.Transfer, 3 }
    };

    [Theory]
    [MemberData(nameof(StableValues))]
    public void Values_ShouldHaveStableNumericRepresentations(
        TransactionType transactionType,
        int expectedValue)
    {
        Assert.Equal(expectedValue, (int)transactionType);
    }

    [Fact]
    public void Enum_ShouldContainExpectedValuesOnly()
    {
        TransactionType[] expectedValues =
        [
            TransactionType.Income,
            TransactionType.Expense,
            TransactionType.Transfer
        ];

        var actualValues = Enum.GetValues<TransactionType>();

        Assert.Equal(expectedValues, actualValues);
    }

    [Fact]
    public void DefaultValue_ShouldNotRepresentValidTransactionType()
    {
        var defaultValue = default(TransactionType);

        Assert.False(Enum.IsDefined(defaultValue));
    }
}
