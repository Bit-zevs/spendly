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

    public static TheoryData<CategoryType> ValidCategoryTypes { get; } = new()
    {
        CategoryType.Income,
        CategoryType.Expense
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

    public static TheoryData<CategoryType> InvalidCategoryTypes { get; } = new()
    {
        (CategoryType)0,
        (CategoryType)(-1),
        (CategoryType)3,
        (CategoryType)999,
        (CategoryType)int.MaxValue
    };

    [Fact]
    public void Create_ShouldCreateCategory_WhenArgumentsAreValid()
    {
        var category = Category.Create(
            ValidName,
            CategoryType.Expense,
            ValidCreatedAt);

        Assert.NotEqual(default(CategoryId), category.Id);
        Assert.NotEqual(Guid.Empty, category.Id.Value);
        Assert.Equal(7, category.Id.Value.Version);

        Assert.Equal(ValidName, category.Name);
        Assert.Equal(CategoryType.Expense, category.Type);
        Assert.Equal(ValidCreatedAt, category.CreatedAt);
    }

    [Fact]
    public void Create_ShouldGenerateDifferentIds_WhenCalledForDifferentCategories()
    {
        var firstCategory = Category.Create(
            "Groceries",
            CategoryType.Expense,
            ValidCreatedAt);

        var secondCategory = Category.Create(
            "Salary",
            CategoryType.Income,
            ValidCreatedAt);

        Assert.NotEqual(firstCategory.Id, secondCategory.Id);
        Assert.NotEqual(firstCategory.Id.Value, secondCategory.Id.Value);
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        var category = Category.Create(
            "  Groceries  ",
            CategoryType.Expense,
            ValidCreatedAt);

        Assert.Equal("Groceries", category.Name);
    }

    [Theory]
    [MemberData(nameof(ValidCategoryTypes))]
    public void Create_ShouldAcceptType_WhenTypeIsDefined(CategoryType type)
    {
        var category = Category.Create(
            ValidName,
            type,
            ValidCreatedAt);

        Assert.Equal(type, category.Type);
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
    public void Create_ShouldThrowDomainException_WhenTypeIsInvalid(
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
