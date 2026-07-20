using System.Reflection;
using Spendly.Domain.Categories;
using Spendly.Domain.Transactions;
using Spendly.Domain.ValueObjects;
using Spendly.Domain.Wallets;

namespace Spendly.IntegrationTests.Persistence.Compatibility;

public sealed class EfCoreDomainModelShapeTests
{
    [Fact]
    public void PersistedDomainProperties_ShouldNotExposePublicSetters()
    {
        AssertHasNoPublicSetter<Wallet>(nameof(Wallet.Id));
        AssertHasNoPublicSetter<Wallet>(nameof(Wallet.Name));
        AssertHasNoPublicSetter<Wallet>(nameof(Wallet.Type));
        AssertHasNoPublicSetter<Wallet>(nameof(Wallet.Currency));
        AssertHasNoPublicSetter<Wallet>(nameof(Wallet.CreatedAt));

        AssertHasNoPublicSetter<Category>(nameof(Category.Id));
        AssertHasNoPublicSetter<Category>(nameof(Category.Name));
        AssertHasNoPublicSetter<Category>(nameof(Category.Type));
        AssertHasNoPublicSetter<Category>(nameof(Category.CreatedAt));

        AssertHasNoPublicSetter<Transaction>(nameof(Transaction.Id));
        AssertHasNoPublicSetter<Transaction>(nameof(Transaction.Type));
        AssertHasNoPublicSetter<Transaction>(nameof(Transaction.Amount));
        AssertHasNoPublicSetter<Transaction>(nameof(Transaction.WalletId));
        AssertHasNoPublicSetter<Transaction>(nameof(Transaction.CategoryId));
        AssertHasNoPublicSetter<Transaction>(nameof(Transaction.OccurredAt));
        AssertHasNoPublicSetter<Transaction>(nameof(Transaction.Description));
        AssertHasNoPublicSetter<Transaction>(nameof(Transaction.CreatedAt));
        AssertHasNoPublicSetter<Transaction>(nameof(Transaction.UpdatedAt));

        AssertHasNoPublicSetter<Money>(nameof(Money.Amount));
        AssertHasNoPublicSetter<Money>(nameof(Money.Currency));
        AssertHasNoPublicSetter<Currency>(nameof(Currency.Code));
    }

    [Fact]
    public void PersistenceConstructors_ShouldRemainPrivate()
    {
        var moneyConstructor = typeof(Money).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        Assert.NotNull(moneyConstructor);
        Assert.True(moneyConstructor.IsPrivate);

        var transactionConstructor = typeof(Transaction).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types:
            [
                typeof(TransactionId),
                typeof(TransactionType),
                typeof(WalletId),
                typeof(CategoryId),
                typeof(DateTimeOffset),
                typeof(string),
                typeof(DateTimeOffset),
                typeof(DateTimeOffset?)
            ],
            modifiers: null);

        Assert.NotNull(transactionConstructor);
        Assert.True(transactionConstructor.IsPrivate);
    }

    private static void AssertHasNoPublicSetter<TEntity>(string propertyName)
    {
        var property = typeof(TEntity).GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(property);
        Assert.False(
            property.SetMethod?.IsPublic is true,
            $"{typeof(TEntity).Name}.{propertyName} must not expose a public setter.");
    }
}
