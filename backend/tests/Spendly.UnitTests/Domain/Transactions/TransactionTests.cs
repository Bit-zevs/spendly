using Spendly.Domain.Categories;
using Spendly.Domain.Errors;
using Spendly.Domain.Transactions;
using Spendly.Domain.ValueObjects;
using Spendly.Domain.Wallets;
using Spendly.UnitTests.TestUtilities;

namespace Spendly.UnitTests.Domain.Transactions;

public sealed class TransactionTests
{
    private const string ValidDescription = "Weekly groceries";

    private static readonly Money ValidAmount = Money.Positive(
        1_250.50m,
        Currency.Rub);

    private static readonly WalletId ValidWalletId = WalletId.From(
        Guid.Parse("0190d4b8-5c2e-7b1f-8f65-3df4a6c90872"));

    private static readonly DateTimeOffset ValidOccurredAt = new(
        2026,
        7,
        10,
        18,
        15,
        0,
        TimeSpan.Zero);

    private static readonly DateTimeOffset ValidCreatedAt = new(
        2026,
        7,
        11,
        12,
        30,
        0,
        TimeSpan.Zero);

    public static TheoryData<string?, string?> DescriptionCases { get; } = new()
    {
        { null!, null! },
        { string.Empty, null! },
        { " ", null! },
        { "   ", null! },
        { "\t\r\n", null! },
        { "\u00A0", null! },
        { ValidDescription, ValidDescription },
        { $"  {ValidDescription}  ", ValidDescription },
        { $"\t{ValidDescription}\t", ValidDescription },
        { $"\u2003{ValidDescription}\u2003", ValidDescription }
    };

    public static TheoryData<TransactionType> InvalidTransactionTypes
        { get; } = new()
    {
        (TransactionType)int.MinValue,
        (TransactionType)(-1),
        (TransactionType)0,
        (TransactionType)4,
        (TransactionType)999,
        (TransactionType)int.MaxValue
    };

    public static TheoryData<TransactionType, CategoryType>
        MismatchedCategoryTypes { get; } = new()
    {
        {
            TransactionType.Income,
            CategoryType.Expense
        },
        {
            TransactionType.Expense,
            CategoryType.Income
        }
    };

    public static TheoryData<DateTimeOffset, DateTimeOffset> UtcCases
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
                17,
                30,
                0,
                TimeSpan.FromHours(5)),
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
                5,
                30,
                0,
                TimeSpan.FromHours(-7)),
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
                12,
                2,
                30,
                0,
                TimeSpan.FromHours(14)),
            new DateTimeOffset(
                2026,
                7,
                11,
                12,
                30,
                0,
                TimeSpan.Zero)
        }
    };

    [Fact]
    public void Create_ShouldInitializeExpense_WhenArgumentsAreValid()
    {
        var category = CreateCategory(CategoryType.Expense);

        var transaction = Transaction.Create(
            TransactionType.Expense,
            ValidAmount,
            ValidWalletId,
            category,
            ValidOccurredAt,
            ValidDescription,
            ValidCreatedAt);

        Assert.NotEqual(default(TransactionId), transaction.Id);
        Assert.NotEqual(Guid.Empty, transaction.Id.Value);
        Assert.Equal(7, transaction.Id.Value.Version);

        Assert.Equal(TransactionType.Expense, transaction.Type);
        Assert.Equal(ValidAmount, transaction.Amount);
        Assert.Equal(ValidAmount.Amount, transaction.Amount.Amount);
        Assert.Equal(ValidAmount.Currency, transaction.Amount.Currency);
        Assert.Equal(ValidWalletId, transaction.WalletId);

        Assert.True(transaction.CategoryId.HasValue);
        Assert.Equal(category.Id, transaction.CategoryId.Value);

        Assert.Equal(ValidOccurredAt, transaction.OccurredAt);
        Assert.Equal(TimeSpan.Zero, transaction.OccurredAt.Offset);
        Assert.Equal(ValidDescription, transaction.Description);
        Assert.Equal(ValidCreatedAt, transaction.CreatedAt);
        Assert.Equal(TimeSpan.Zero, transaction.CreatedAt.Offset);
        Assert.Null(transaction.UpdatedAt);
    }

    [Fact]
    public void Create_ShouldInitializeIncome_WhenArgumentsAreValid()
    {
        var category = CreateCategory(CategoryType.Income);

        var transaction = Transaction.Create(
            TransactionType.Income,
            Money.Positive(75_000m, Currency.Rub),
            ValidWalletId,
            category,
            ValidOccurredAt,
            "Monthly salary",
            ValidCreatedAt);

        Assert.Equal(TransactionType.Income, transaction.Type);
        Assert.True(transaction.CategoryId.HasValue);
        Assert.Equal(category.Id, transaction.CategoryId.Value);
        Assert.Null(transaction.UpdatedAt);
    }

    [Fact]
    public void Create_ShouldGenerateDifferentIds_WhenBusinessDataIsIdentical()
    {
        var category = CreateCategory(CategoryType.Expense);

        var firstTransaction = Transaction.Create(
            TransactionType.Expense,
            ValidAmount,
            ValidWalletId,
            category,
            ValidOccurredAt,
            ValidDescription,
            ValidCreatedAt);

        var secondTransaction = Transaction.Create(
            TransactionType.Expense,
            ValidAmount,
            ValidWalletId,
            category,
            ValidOccurredAt,
            ValidDescription,
            ValidCreatedAt);

        Assert.NotEqual(firstTransaction.Id, secondTransaction.Id);
        Assert.NotEqual(
            firstTransaction.Id.Value,
            secondTransaction.Id.Value);
    }

    [Theory]
    [MemberData(nameof(DescriptionCases))]
    public void Create_ShouldNormalizeDescription(
        string? description,
        string? expectedDescription)
    {
        var transaction = Transaction.Create(
            TransactionType.Expense,
            ValidAmount,
            ValidWalletId,
            CreateCategory(CategoryType.Expense),
            ValidOccurredAt,
            description,
            ValidCreatedAt);

        Assert.Equal(expectedDescription, transaction.Description);
    }

    [Theory]
    [MemberData(nameof(UtcCases))]
    public void Create_ShouldStoreOccurredAtInUtc(
        DateTimeOffset occurredAt,
        DateTimeOffset expectedOccurredAt)
    {
        var transaction = Transaction.Create(
            TransactionType.Expense,
            ValidAmount,
            ValidWalletId,
            CreateCategory(CategoryType.Expense),
            occurredAt,
            ValidDescription,
            ValidCreatedAt);

        Assert.Equal(expectedOccurredAt, transaction.OccurredAt);
        Assert.Equal(TimeSpan.Zero, transaction.OccurredAt.Offset);
    }

    [Theory]
    [MemberData(nameof(UtcCases))]
    public void Create_ShouldStoreCreatedAtInUtc(
        DateTimeOffset createdAt,
        DateTimeOffset expectedCreatedAt)
    {
        var transaction = Transaction.Create(
            TransactionType.Expense,
            ValidAmount,
            ValidWalletId,
            CreateCategory(CategoryType.Expense),
            ValidOccurredAt,
            ValidDescription,
            createdAt);

        Assert.Equal(expectedCreatedAt, transaction.CreatedAt);
        Assert.Equal(TimeSpan.Zero, transaction.CreatedAt.Offset);
    }

    [Theory]
    [MemberData(nameof(InvalidTransactionTypes))]
    public void Create_ShouldThrowDomainException_WhenTypeIsUndefined(
        TransactionType type)
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Transaction.TypeIsInvalid,
            () => Transaction.Create(
                type,
                ValidAmount,
                ValidWalletId,
                CreateCategory(CategoryType.Expense),
                ValidOccurredAt,
                ValidDescription,
                ValidCreatedAt));
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenTypeIsTransfer()
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Transaction.TransferIsNotSupported,
            () => Transaction.Create(
                TransactionType.Transfer,
                ValidAmount,
                ValidWalletId,
                CreateCategory(CategoryType.Expense),
                ValidOccurredAt,
                ValidDescription,
                ValidCreatedAt));
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenAmountIsNull()
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Transaction.AmountIsRequired,
            () => Transaction.Create(
                TransactionType.Expense,
                null,
                ValidWalletId,
                CreateCategory(CategoryType.Expense),
                ValidOccurredAt,
                ValidDescription,
                ValidCreatedAt));
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenAmountIsZero()
    {
        var zeroAmount = Money.Zero(Currency.Rub);

        DomainExceptionAssert.Throws(
            DomainErrors.Transaction.AmountMustBePositive,
            () => Transaction.Create(
                TransactionType.Expense,
                zeroAmount,
                ValidWalletId,
                CreateCategory(CategoryType.Expense),
                ValidOccurredAt,
                ValidDescription,
                ValidCreatedAt));
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenWalletIdIsDefault()
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Transaction.WalletIsRequired,
            () => Transaction.Create(
                TransactionType.Expense,
                ValidAmount,
                default,
                CreateCategory(CategoryType.Expense),
                ValidOccurredAt,
                ValidDescription,
                ValidCreatedAt));
    }

    [Theory]
    [InlineData(TransactionType.Income)]
    [InlineData(TransactionType.Expense)]
    public void Create_ShouldThrowDomainException_WhenCategoryIsNull(
        TransactionType type)
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Transaction.CategoryIsRequired,
            () => Transaction.Create(
                type,
                ValidAmount,
                ValidWalletId,
                null,
                ValidOccurredAt,
                ValidDescription,
                ValidCreatedAt));
    }

    [Theory]
    [MemberData(nameof(MismatchedCategoryTypes))]
    public void Create_ShouldThrowDomainException_WhenCategoryTypeDoesNotMatch(
        TransactionType transactionType,
        CategoryType categoryType)
    {
        var category = CreateCategory(categoryType);

        DomainExceptionAssert.Throws(
            DomainErrors.Transaction.CategoryTypeMismatch,
            () => Transaction.Create(
                transactionType,
                ValidAmount,
                ValidWalletId,
                category,
                ValidOccurredAt,
                ValidDescription,
                ValidCreatedAt));
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenOccurredAtIsDefault()
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Transaction.OccurredAtIsInvalid,
            () => Transaction.Create(
                TransactionType.Expense,
                ValidAmount,
                ValidWalletId,
                CreateCategory(CategoryType.Expense),
                default,
                ValidDescription,
                ValidCreatedAt));
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenCreatedAtIsDefault()
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Transaction.CreatedAtIsInvalid,
            () => Transaction.Create(
                TransactionType.Expense,
                ValidAmount,
                ValidWalletId,
                CreateCategory(CategoryType.Expense),
                ValidOccurredAt,
                ValidDescription,
                default));
    }

    private static Category CreateCategory(CategoryType type)
    {
        var name = type switch
        {
            CategoryType.Income => "Salary",
            CategoryType.Expense => "Groceries",
            _ => throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "Unsupported category type.")
        };

        return Category.Create(
            name,
            type,
            ValidCreatedAt);
    }
}
