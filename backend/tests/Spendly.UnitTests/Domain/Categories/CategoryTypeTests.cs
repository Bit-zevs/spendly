using Spendly.Domain.Categories;

namespace Spendly.UnitTests.Domain.Categories;

public sealed class CategoryTypeTests
{
    public static TheoryData<CategoryType, int> StableValues => new()
    {
        { CategoryType.Income, 1 },
        { CategoryType.Expense, 2 }
    };

    [Theory]
    [MemberData(nameof(StableValues))]
    public void Values_ShouldHaveStableNumericRepresentations(
        CategoryType categoryType,
        int expectedValue)
    {
        Assert.Equal(expectedValue, (int)categoryType);
    }

    [Fact]
    public void Enum_ShouldContainExpectedValuesOnly()
    {
        CategoryType[] expectedValues =
        [
            CategoryType.Income,
            CategoryType.Expense
        ];

        var actualValues = Enum.GetValues<CategoryType>();

        Assert.Equal(expectedValues, actualValues);
    }

    [Fact]
    public void DefaultValue_ShouldNotRepresentValidCategoryType()
    {
        const CategoryType defaultValue = 0;

        Assert.False(Enum.IsDefined(defaultValue));
    }
}
