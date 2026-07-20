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

    private static readonly Wallet ValidWallet = Wallet.Create(
        "Main wallet",
        WalletType.DebitCard,
        Currency.Rub,
        new DateTimeOffset(
            2026,
            7,
            11,
            12,
            0,
            0,
            TimeSpan.Zero));

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

    public static TheoryData<string?, string?> DescriptionNormalizationCases { get; } = new()
    {
        { null, null },
        { string.Empty, null },
        { " ", null },
        { "   ", null },
        { "\t\r\n", null },
        { "\u00A0", null },
        { ValidDescription, ValidDescription },
        { $"  {ValidDescription}  ", ValidDescription },
        { $"\t{ValidDescription}\t", ValidDescription },
        { $"\u2003{ValidDescription}\u2003", ValidDescription }
    };

    public static TheoryData<TransactionType> InvalidTransactionTypes { get; } = new()
    {
        (TransactionType)int.MinValue,
        (TransactionType)(-1),
        (TransactionType)0,
        (TransactionType)4,
        (TransactionType)999,
        (TransactionType)int.MaxValue
    };

    public static TheoryData<TransactionType, CategoryType> MismatchedCategoryTypes { get; } = new()
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

    public static TheoryData<DateTimeOffset, DateTimeOffset> DateNormalizationCases { get; } = new()
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
    public void Create_ShouldCreateExpense_WhenArgumentsAreValid()
    {
        var category = CreateCategory(CategoryType.Expense);

        var transaction = Transaction.Create(
            TransactionType.Expense,
            ValidAmount,
            ValidWallet,
            category,
            ValidOccurredAt,
            ValidDescription,
            ValidCreatedAt);

        AssertCreatedTransaction(
            transaction,
            TransactionType.Expense,
            ValidAmount,
            ValidWallet,
            category.Id,
            ValidOccurredAt,
            ValidDescription,
            ValidCreatedAt);
    }

    [Fact]
    public void Create_ShouldCreateIncome_WhenArgumentsAreValid()
    {
        var amount = Money.Positive(
            75_000m,
            Currency.Rub);

        var category = CreateCategory(CategoryType.Income);

        const string description = "Monthly salary";

        var transaction = Transaction.Create(
            TransactionType.Income,
            amount,
            ValidWallet,
            category,
            ValidOccurredAt,
            description,
            ValidCreatedAt);

        AssertCreatedTransaction(
            transaction,
            TransactionType.Income,
            amount,
            ValidWallet,
            category.Id,
            ValidOccurredAt,
            description,
            ValidCreatedAt);
    }

    [Fact]
    public void Create_ShouldGenerateDifferentIds_WhenBusinessDataIsIdentical()
    {
        var category = CreateCategory(CategoryType.Expense);

        var firstTransaction = Transaction.Create(
            TransactionType.Expense,
            ValidAmount,
            ValidWallet,
            category,
            ValidOccurredAt,
            ValidDescription,
            ValidCreatedAt);

        var secondTransaction = Transaction.Create(
            TransactionType.Expense,
            ValidAmount,
            ValidWallet,
            category,
            ValidOccurredAt,
            ValidDescription,
            ValidCreatedAt);

        Assert.NotEqual(
            firstTransaction.Id,
            secondTransaction.Id);

        Assert.NotEqual(
            firstTransaction.Id.Value,
            secondTransaction.Id.Value);
    }

    [Theory]
    [MemberData(nameof(DescriptionNormalizationCases))]
    public void Create_ShouldNormalizeDescription(
        string? description,
        string? expectedDescription)
    {
        var transaction = CreateValidExpense(
            description: description);

        Assert.Equal(
            expectedDescription,
            transaction.Description);
    }

    [Fact]
    public void Create_ShouldAcceptDescription_WhenLengthEqualsMaximum()
    {
        var description = new string(
            'a',
            Transaction.MaxDescriptionLength);

        var transaction = CreateValidExpense(
            description: description);

        Assert.Equal(
            description,
            transaction.Description);
    }

    [Fact]
    public void Create_ShouldValidateDescriptionLength_AfterTrimming()
    {
        var expectedDescription = new string(
            'a',
            Transaction.MaxDescriptionLength);

        var description = $"  {expectedDescription}  ";

        var transaction = CreateValidExpense(
            description: description);

        Assert.Equal(
            expectedDescription,
            transaction.Description);
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenDescriptionIsTooLong()
    {
        var description = new string(
            'a',
            Transaction.MaxDescriptionLength + 1);

        DomainExceptionAssert.Throws(
            DomainErrors.Transaction.DescriptionIsTooLong,
            () => CreateValidExpense(
                description: description));
    }

    [Theory]
    [MemberData(nameof(DateNormalizationCases))]
    public void Create_ShouldStoreOccurredAtInUtc(
        DateTimeOffset occurredAt,
        DateTimeOffset expectedOccurredAt)
    {
        var transaction = CreateValidExpense(
            occurredAt: occurredAt);

        Assert.Equal(
            expectedOccurredAt,
            transaction.OccurredAt);

        Assert.Equal(
            TimeSpan.Zero,
            transaction.OccurredAt.Offset);
    }

    [Theory]
    [MemberData(nameof(DateNormalizationCases))]
    public void Create_ShouldStoreCreatedAtInUtc(
        DateTimeOffset createdAt,
        DateTimeOffset expectedCreatedAt)
    {
        var transaction = CreateValidExpense(
            createdAt: createdAt);

        Assert.Equal(
            expectedCreatedAt,
            transaction.CreatedAt);

        Assert.Equal(
            TimeSpan.Zero,
            transaction.CreatedAt.Offset);
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
                ValidWallet,
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
                ValidWallet,
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
                ValidWallet,
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
                ValidWallet,
                CreateCategory(CategoryType.Expense),
                ValidOccurredAt,
                ValidDescription,
                ValidCreatedAt));
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenWalletIsNull()
    {
        DomainExceptionAssert.Throws(
            DomainErrors.Transaction.WalletIsRequired,
            () => Transaction.Create(
                TransactionType.Expense,
                ValidAmount,
                null,
                CreateCategory(CategoryType.Expense),
                ValidOccurredAt,
                ValidDescription,
                ValidCreatedAt));
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenAmountCurrencyDoesNotMatchWalletCurrency()
    {
        var usdAmount = Money.Positive(100m, Currency.Usd);

        DomainExceptionAssert.Throws(
            DomainErrors.Transaction.AmountCurrencyMismatch,
            () => Transaction.Create(
                TransactionType.Expense,
                usdAmount,
                ValidWallet,
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
                ValidWallet,
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
                ValidWallet,
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
                ValidWallet,
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
                ValidWallet,
                CreateCategory(CategoryType.Expense),
                ValidOccurredAt,
                ValidDescription,
                default));
    }

    private static Transaction CreateValidExpense(
        string? description = ValidDescription,
        DateTimeOffset? occurredAt = null,
        DateTimeOffset? createdAt = null)
    {
        return Transaction.Create(
            TransactionType.Expense,
            ValidAmount,
            ValidWallet,
            CreateCategory(CategoryType.Expense),
            occurredAt ?? ValidOccurredAt,
            description,
            createdAt ?? ValidCreatedAt);
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

    private static void AssertCreatedTransaction(
        Transaction transaction,
        TransactionType expectedType,
        Money expectedAmount,
        Wallet expectedWallet,
        CategoryId expectedCategoryId,
        DateTimeOffset expectedOccurredAt,
        string? expectedDescription,
        DateTimeOffset expectedCreatedAt)
    {
        Assert.NotEqual(
            default(TransactionId),
            transaction.Id);

        Assert.NotEqual(
            Guid.Empty,
            transaction.Id.Value);

        Assert.Equal(
            7,
            transaction.Id.Value.Version);

        Assert.Equal(
            expectedType,
            transaction.Type);

        Assert.Equal(
            expectedAmount,
            transaction.Amount);

        Assert.Equal(
            expectedAmount.Amount,
            transaction.Amount.Amount);

        Assert.Equal(
            expectedAmount.Currency,
            transaction.Amount.Currency);

        Assert.Equal(
            expectedWallet.Id,
            transaction.WalletId);

        Assert.Equal(
            expectedCategoryId,
            transaction.CategoryId);

        Assert.Equal(
            expectedOccurredAt.ToUniversalTime(),
            transaction.OccurredAt);

        Assert.Equal(
            TimeSpan.Zero,
            transaction.OccurredAt.Offset);

        Assert.Equal(
            expectedDescription,
            transaction.Description);

        Assert.Equal(
            expectedCreatedAt.ToUniversalTime(),
            transaction.CreatedAt);

        Assert.Equal(
            TimeSpan.Zero,
            transaction.CreatedAt.Offset);

        Assert.Null(transaction.UpdatedAt);
    }
}
