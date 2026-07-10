using Spendly.Domain.Categories;
using Spendly.Domain.Errors;
using Spendly.UnitTests.TestUtilities;

namespace Spendly.UnitTests.Domain.Categories;

public sealed class CategoryTests
{
    private const string ValidName = "Groceries";

    private static readonly DateTimeOffset ValidCreatedAt = new(
        2026,
        7,
        11,
        12,
        30,
        0,
        TimeSpan.Zero);

    public static TheoryData<string, string> NamesToNormalize { get; } = new()
    {
        { ValidName, ValidName },
        { " Groceries ", ValidName },
        { "   Groceries   ", ValidName },
        { "\tGroceries\t", ValidName },
        { "\r\nGroceries\r\n", ValidName },
        { "\u00A0Groceries\u00A0", ValidName },
        { "\u2003Groceries\u2003", ValidName }
    };

    public static TheoryData<CategoryType> ValidCategoryTypes { get; } = new()
    {
        CategoryType.Income,
        CategoryType.Expense
    };

    public static TheoryData<DateTimeOffset, DateTimeOffset> CreatedAtUtcCases
        { get; } = new()
    {
        {
            new DateTimeOffset(
                2026,
                7,
                11,
                12,
                30,
                0,
                TimeSpan.Zero),
            new DateTimeOffset(
                2026,
                7,
                11,
                12,
                30,
                0,
                TimeSpan.Zero)
        },
        {
            new DateTimeOffset(
                2026,
                7,
                11,
                15,
                30,
                0,
                TimeSpan.FromHours(5)),
            new DateTimeOffset(
                2026,
                7,
                11,
                10,
                30,
                0,
                TimeSpan.Zero)
        },
        {
            new DateTimeOffset(
                2026,
                7,
                11,
                4,
                30,
                0,
                TimeSpan.FromHours(-7)),
            new DateTimeOffset(
                2026,
                7,
                11,
                11,
                30,
                0,
                TimeSpan.Zero)
        },
        {
            new DateTimeOffset(
                2026,
                7,
                11,
                23,
                30,
                0,
                TimeSpan.FromHours(14)),
            new DateTimeOffset(
                2026,
                7,
                11,
                9,
                30,
                0,
                TimeSpan.Zero)
        }
    };

    public static TheoryData<string?> InvalidNames { get; } = new()
    {
        null!,
        string.Empty,
        " ",
        "   ",
        "\t",
        "\r\n",
        "\u00A0",
        "\u2003"
    };

    public static TheoryData<CategoryType> InvalidCategoryTypes { get; } = new()
    {
        (CategoryType)int.MinValue,
        (CategoryType)(-1),
        (CategoryType)0,
        (CategoryType)3,
        (CategoryType)999,
        (CategoryType)int.MaxValue
    };

    [Fact]
    public void Create_ShouldInitializeCategory_WhenArgumentsAreValid()
    {
        var category = Category.Create(
            ValidName,
            CategoryType.Expense,
            ValidCreatedAt);

        Assert.NotEqual(default(CategoryId), category.Id);
        Assert.NotEqual(Guid.Empty, category.Id.Value);
        Assert.Equal(ValidName, category.Name);
        Assert.Equal(CategoryType.Expense, category.Type);
        Assert.Equal(ValidCreatedAt, category.CreatedAt);
        Assert.Equal(TimeSpan.Zero, category.CreatedAt.Offset);
    }

    [Fact]
    public void Create_ShouldGenerateDifferentIds_WhenBusinessDataIsIdentical()
    {
        var firstCategory = Category.Create(
            ValidName,
            CategoryType.Expense,
            ValidCreatedAt);

        var secondCategory = Category.Create(
            ValidName,
            CategoryType.Expense,
            ValidCreatedAt);

        Assert.NotEqual(firstCategory.Id, secondCategory.Id);
        Assert.NotEqual(firstCategory.Id.Value, secondCategory.Id.Value);
    }

    [Theory]
    [MemberData(nameof(NamesToNormalize))]
    public void Create_ShouldStoreTrimmedName(
        string name,
        string expectedName)
    {
        var category = Category.Create(
            name,
            CategoryType.Expense,
            ValidCreatedAt);

        Assert.Equal(expectedName, category.Name);
    }

    [Theory]
    [MemberData(nameof(ValidCategoryTypes))]
    public void Create_ShouldAcceptType_WhenTypeIsDefined(
        CategoryType type)
    {
        var category = Category.Create(
            ValidName,
            type,
            ValidCreatedAt);

        Assert.Equal(type, category.Type);
    }

    [Theory]
    [MemberData(nameof(CreatedAtUtcCases))]
    public void Create_ShouldStoreCreatedAtInUtc(
        DateTimeOffset createdAt,
        DateTimeOffset expectedCreatedAt)
    {
        var category = Category.Create(
            ValidName,
            CategoryType.Expense,
            createdAt);

        Assert.Equal(expectedCreatedAt, category.CreatedAt);
        Assert.Equal(TimeSpan.Zero, category.CreatedAt.Offset);
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void Create_ShouldThrowDomainException_WhenNameIsNullOrWhiteSpace(
        string? name)
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Category.NameIsEmpty,
            () => Category.Create(
                name,
                CategoryType.Expense,
                ValidCreatedAt));
    }

    [Theory]
    [MemberData(nameof(InvalidCategoryTypes))]
    public void Create_ShouldThrowDomainException_WhenTypeIsUndefined(
        CategoryType type)
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Category.TypeIsInvalid,
            () => Category.Create(
                ValidName,
                type,
                ValidCreatedAt));
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenCreatedAtIsDefault()
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Category.CreatedAtIsInvalid,
            () => Category.Create(
                ValidName,
                CategoryType.Expense,
                default));
    }
}
