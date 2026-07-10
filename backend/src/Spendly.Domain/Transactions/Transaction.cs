using Spendly.Domain.Categories;
using Spendly.Domain.Common;
using Spendly.Domain.Errors;
using Spendly.Domain.ValueObjects;
using Spendly.Domain.Wallets;

namespace Spendly.Domain.Transactions;

public sealed class Transaction : Entity<TransactionId>
{
    private Transaction(
        TransactionId id,
        TransactionType type,
        Money amount,
        WalletId walletId,
        CategoryId? categoryId,
        DateTimeOffset occurredAt,
        string? description,
        DateTimeOffset createdAt)
        : base(id)
    {
        Type = type;
        Amount = amount;
        WalletId = walletId;
        CategoryId = categoryId;
        OccurredAt = occurredAt;
        Description = description;
        CreatedAt = createdAt;
        UpdatedAt = null;
    }

    public TransactionType Type { get; }

    public Money Amount { get; }

    public WalletId WalletId { get; }

    public CategoryId? CategoryId { get; }

    public DateTimeOffset OccurredAt { get; }

    public string? Description { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? UpdatedAt { get; }

    public static Transaction Create(
        TransactionType type,
        Money? amount,
        WalletId walletId,
        Category? category,
        DateTimeOffset occurredAt,
        string? description,
        DateTimeOffset createdAt)
    {
        EnsureTypeIsValid(type);
        EnsureTypeIsSupported(type);

        var requiredAmount = EnsureAmountIsValid(amount);

        EnsureWalletIsProvided(walletId);

        var requiredCategory = EnsureCategoryIsValid(
            type,
            category);

        var utcOccurredAt = NormalizeOccurredAt(occurredAt);
        var normalizedDescription = NormalizeDescription(description);
        var utcCreatedAt = NormalizeCreatedAt(createdAt);

        return new Transaction(
            TransactionId.New(),
            type,
            requiredAmount,
            walletId,
            requiredCategory.Id,
            utcOccurredAt,
            normalizedDescription,
            utcCreatedAt);
    }

    private static void EnsureTypeIsValid(TransactionType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException(
                DomainErrors.Transaction.TypeIsInvalid);
        }
    }

    private static void EnsureTypeIsSupported(TransactionType type)
    {
        if (type is TransactionType.Transfer)
        {
            throw new DomainException(
                DomainErrors.Transaction.TransferIsNotSupported);
        }
    }

    private static Money EnsureAmountIsValid(Money? amount)
    {
        if (amount is null)
        {
            throw new DomainException(
                DomainErrors.Transaction.AmountIsRequired);
        }

        if (!amount.IsPositive)
        {
            throw new DomainException(
                DomainErrors.Transaction.AmountMustBePositive);
        }

        return amount;
    }

    private static void EnsureWalletIsProvided(WalletId walletId)
    {
        if (walletId == default)
        {
            throw new DomainException(
                DomainErrors.Transaction.WalletIsRequired);
        }
    }

    private static Category EnsureCategoryIsValid(
        TransactionType type,
        Category? category)
    {
        if (category is null)
        {
            throw new DomainException(
                DomainErrors.Transaction.CategoryIsRequired);
        }

        var expectedCategoryType = type switch
        {
            TransactionType.Income => CategoryType.Income,
            TransactionType.Expense => CategoryType.Expense,
            _ => throw new DomainException(
                DomainErrors.Transaction.TransferIsNotSupported)
        };

        if (category.Type != expectedCategoryType)
        {
            throw new DomainException(
                DomainErrors.Transaction.CategoryTypeMismatch);
        }

        return category;
    }

    private static DateTimeOffset NormalizeOccurredAt(
        DateTimeOffset occurredAt)
    {
        if (occurredAt == default)
        {
            throw new DomainException(
                DomainErrors.Transaction.OccurredAtIsInvalid);
        }

        return occurredAt.ToUniversalTime();
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }

    private static DateTimeOffset NormalizeCreatedAt(
        DateTimeOffset createdAt)
    {
        if (createdAt == default)
        {
            throw new DomainException(
                DomainErrors.Transaction.CreatedAtIsInvalid);
        }

        return createdAt.ToUniversalTime();
    }
}
