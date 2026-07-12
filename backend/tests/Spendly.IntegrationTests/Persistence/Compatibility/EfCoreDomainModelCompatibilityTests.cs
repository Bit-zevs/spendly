using Microsoft.EntityFrameworkCore;
using Spendly.Domain.Categories;
using Spendly.Domain.Transactions;
using Spendly.Domain.ValueObjects;
using Spendly.Domain.Wallets;
using Testcontainers.PostgreSql;

namespace Spendly.IntegrationTests.Persistence.Compatibility;

public sealed class EfCoreDomainModelCompatibilityTests
{
    private const string PostgreSqlImage = "postgres:17.10";

    [Fact]
    [Trait("Dependency", "Docker")]
    public async Task DomainModel_ShouldRoundTripThroughPostgreSql()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var postgreSql = new PostgreSqlBuilder(PostgreSqlImage)
            .WithDatabase("spendly_compatibility")
            .WithUsername("spendly")
            .WithPassword("spendly_password")
            .Build();

        await postgreSql.StartAsync(cancellationToken);

        var options = new DbContextOptionsBuilder<EfCoreCompatibilityDbContext>()
            .UseNpgsql(postgreSql.GetConnectionString())
            .EnableDetailedErrors()
            .Options;

        var wallet = CreateWallet();
        var category = CreateCategory();

        var updatedTransaction = CreateTransaction(
            wallet,
            category,
            amount: 125.75m,
            description: "  Groceries  ",
            occurredAt: new DateTimeOffset(
                2026,
                7,
                10,
                18,
                30,
                15,
                123,
                TimeSpan.FromHours(3)));

        var unchangedTransaction = CreateTransaction(
            wallet,
            category,
            amount: 20m,
            description: null,
            occurredAt: new DateTimeOffset(
                2026,
                7,
                11,
                12,
                10,
                30,
                456,
                TimeSpan.FromHours(5)));

        var expectedUpdatedAt = new DateTimeOffset(
            2026,
            7,
            12,
            8,
            45,
            10,
            789,
            TimeSpan.Zero);

        await SaveDomainObjectsAsync(
            options,
            wallet,
            category,
            updatedTransaction,
            unchangedTransaction,
            expectedUpdatedAt,
            cancellationToken);

        await AssertMaterializedObjectsAsync(
            options,
            wallet,
            category,
            updatedTransaction,
            unchangedTransaction,
            expectedUpdatedAt,
            cancellationToken);
    }

    private static Wallet CreateWallet()
    {
        return Wallet.Create(
            name: "  Main wallet  ",
            type: WalletType.DebitCard,
            currency: Currency.Usd,
            createdAt: new DateTimeOffset(
                2026,
                7,
                8,
                10,
                15,
                20,
                123,
                TimeSpan.FromHours(5)));
    }

    private static Category CreateCategory()
    {
        return Category.Create(
            name: "  Groceries  ",
            type: CategoryType.Expense,
            createdAt: new DateTimeOffset(
                2026,
                7,
                8,
                11,
                30,
                40,
                456,
                TimeSpan.FromHours(5)));
    }

    private static Transaction CreateTransaction(
        Wallet wallet,
        Category category,
        decimal amount,
        string? description,
        DateTimeOffset occurredAt)
    {
        return Transaction.Create(
            type: TransactionType.Expense,
            amount: Money.Positive(amount, Currency.Usd),
            walletId: wallet.Id,
            category: category,
            occurredAt: occurredAt,
            description: description,
            createdAt: new DateTimeOffset(
                2026,
                7,
                12,
                14,
                20,
                30,
                123,
                TimeSpan.FromHours(5)));
    }

    private static async Task SaveDomainObjectsAsync(
        DbContextOptions<EfCoreCompatibilityDbContext> options,
        Wallet wallet,
        Category category,
        Transaction updatedTransaction,
        Transaction unchangedTransaction,
        DateTimeOffset expectedUpdatedAt,
        CancellationToken cancellationToken)
    {
        await using var writeContext =
            new EfCoreCompatibilityDbContext(options);

        await writeContext.Database.EnsureCreatedAsync(cancellationToken);

        writeContext.Wallets.Add(wallet);
        writeContext.Categories.Add(category);

        await writeContext.SaveChangesAsync(cancellationToken);

        writeContext.Transactions.AddRange(
            updatedTransaction,
            unchangedTransaction);

        await writeContext.SaveChangesAsync(cancellationToken);

        FormattableString updateSql =
            $"""
             UPDATE transactions
             SET updated_at = {expectedUpdatedAt}
             WHERE id = {updatedTransaction.Id.Value};
             """;

        var affectedRows =
            await writeContext.Database.ExecuteSqlInterpolatedAsync(
                updateSql,
                cancellationToken);

        Assert.Equal(1, affectedRows);
    }

    private static async Task AssertMaterializedObjectsAsync(
        DbContextOptions<EfCoreCompatibilityDbContext> options,
        Wallet expectedWallet,
        Category expectedCategory,
        Transaction expectedUpdatedTransaction,
        Transaction expectedUnchangedTransaction,
        DateTimeOffset expectedUpdatedAt,
        CancellationToken cancellationToken)
    {
        await using var readContext =
            new EfCoreCompatibilityDbContext(options);

        var actualWallet = await readContext.Wallets
            .AsNoTracking()
            .SingleAsync(
                wallet => wallet.Id == expectedWallet.Id,
                cancellationToken);

        var actualCategory = await readContext.Categories
            .AsNoTracking()
            .SingleAsync(
                category => category.Id == expectedCategory.Id,
                cancellationToken);

        var actualUpdatedTransaction = await readContext.Transactions
            .AsNoTracking()
            .SingleAsync(
                transaction =>
                    transaction.Id == expectedUpdatedTransaction.Id,
                cancellationToken);

        var actualUnchangedTransaction = await readContext.Transactions
            .AsNoTracking()
            .SingleAsync(
                transaction =>
                    transaction.Id == expectedUnchangedTransaction.Id,
                cancellationToken);

        AssertWallet(expectedWallet, actualWallet);
        AssertCategory(expectedCategory, actualCategory);

        AssertTransaction(
            expectedUpdatedTransaction,
            actualUpdatedTransaction);

        Assert.Equal(
            expectedUpdatedAt,
            actualUpdatedTransaction.UpdatedAt);

        Assert.Equal(
            TimeSpan.Zero,
            actualUpdatedTransaction.UpdatedAt!.Value.Offset);

        AssertTransaction(
            expectedUnchangedTransaction,
            actualUnchangedTransaction);

        Assert.Null(actualUnchangedTransaction.UpdatedAt);

        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    private static void AssertWallet(
        Wallet expected,
        Wallet actual)
    {
        Assert.NotSame(expected, actual);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Type, actual.Type);
        Assert.Equal(expected.Currency, actual.Currency);
        Assert.Same(Currency.Usd, actual.Currency);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(TimeSpan.Zero, actual.CreatedAt.Offset);
    }

    private static void AssertCategory(
        Category expected,
        Category actual)
    {
        Assert.NotSame(expected, actual);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Type, actual.Type);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(TimeSpan.Zero, actual.CreatedAt.Offset);
    }

    private static void AssertTransaction(
        Transaction expected,
        Transaction actual)
    {
        Assert.NotSame(expected, actual);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Type, actual.Type);
        Assert.Equal(expected.Amount, actual.Amount);
        Assert.Same(Currency.Usd, actual.Amount.Currency);
        Assert.Equal(expected.WalletId, actual.WalletId);
        Assert.Equal(expected.CategoryId, actual.CategoryId);
        Assert.Equal(expected.OccurredAt, actual.OccurredAt);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(TimeSpan.Zero, actual.OccurredAt.Offset);
        Assert.Equal(TimeSpan.Zero, actual.CreatedAt.Offset);
    }
}
