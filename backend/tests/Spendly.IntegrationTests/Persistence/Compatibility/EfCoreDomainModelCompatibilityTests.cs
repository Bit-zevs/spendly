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

    [Fact(Explicit = true)]
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
        var expenseCategory = CreateCategory(
            "Groceries",
            CategoryType.Expense);
        var incomeCategory = CreateCategory(
            "Salary",
            CategoryType.Income);

        var expense = CreateTransaction(
            TransactionType.Expense,
            wallet,
            expenseCategory,
            amount: 125.7500m,
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

        var income = CreateTransaction(
            TransactionType.Income,
            wallet,
            incomeCategory,
            amount: 75_000m,
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

        await SaveDomainObjectsAsync(
            options,
            wallet,
            expenseCategory,
            incomeCategory,
            expense,
            income,
            cancellationToken);

        await AssertMaterializedObjectsAsync(
            options,
            wallet,
            expenseCategory,
            incomeCategory,
            expense,
            income,
            cancellationToken);
    }

    private static Wallet CreateWallet()
    {
        return Wallet.Create(
            name: "  Main wallet  ",
            type: WalletType.DebitCard,
            currency: Currency.From("kzt"),
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

    private static Category CreateCategory(
        string name,
        CategoryType type)
    {
        return Category.Create(
            name: $"  {name}  ",
            type: type,
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
        TransactionType type,
        Wallet wallet,
        Category category,
        decimal amount,
        string? description,
        DateTimeOffset occurredAt)
    {
        return Transaction.Create(
            type: type,
            amount: Money.Positive(amount, wallet.Currency),
            wallet: wallet,
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
        Category expenseCategory,
        Category incomeCategory,
        Transaction expense,
        Transaction income,
        CancellationToken cancellationToken)
    {
        await using var writeContext =
            new EfCoreCompatibilityDbContext(options);

        await writeContext.Database.EnsureCreatedAsync(cancellationToken);

        writeContext.Wallets.Add(wallet);
        writeContext.Categories.AddRange(
            expenseCategory,
            incomeCategory);
        writeContext.Transactions.AddRange(
            expense,
            income);

        await writeContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task AssertMaterializedObjectsAsync(
        DbContextOptions<EfCoreCompatibilityDbContext> options,
        Wallet expectedWallet,
        Category expectedExpenseCategory,
        Category expectedIncomeCategory,
        Transaction expectedExpense,
        Transaction expectedIncome,
        CancellationToken cancellationToken)
    {
        await using var readContext =
            new EfCoreCompatibilityDbContext(options);

        var actualWallet = await readContext.Wallets
            .AsNoTracking()
            .SingleAsync(
                wallet => wallet.Id == expectedWallet.Id,
                cancellationToken);

        var actualCategories = await readContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToArrayAsync(cancellationToken);

        var actualTransactions = await readContext.Transactions
            .AsNoTracking()
            .OrderBy(transaction => transaction.Type)
            .ToArrayAsync(cancellationToken);

        AssertWallet(expectedWallet, actualWallet);

        Assert.Collection(
            actualCategories,
            category => AssertCategory(expectedExpenseCategory, category),
            category => AssertCategory(expectedIncomeCategory, category));

        Assert.Collection(
            actualTransactions,
            transaction => AssertTransaction(expectedIncome, transaction),
            transaction => AssertTransaction(expectedExpense, transaction));

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
        Assert.Equal("KZT", actual.Currency.Code);
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
        Assert.Equal("KZT", actual.Amount.Currency.Code);
        Assert.Equal(expected.WalletId, actual.WalletId);
        Assert.Equal(expected.CategoryId, actual.CategoryId);
        Assert.Equal(expected.OccurredAt, actual.OccurredAt);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(TimeSpan.Zero, actual.OccurredAt.Offset);
        Assert.Equal(TimeSpan.Zero, actual.CreatedAt.Offset);
    }
}
