using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Spendly.Domain.Categories;
using Spendly.Domain.Transactions;
using Spendly.Domain.ValueObjects;
using Spendly.Domain.Wallets;
using Spendly.Infrastructure.Persistence;
using Spendly.Infrastructure.Persistence.DesignTime;
using Testcontainers.PostgreSql;

namespace Spendly.IntegrationTests.Persistence;

public sealed class InitialDatabaseMigrationTests
{
    private const string PostgreSqlImage = "postgres:17.10";

    private static readonly string[] ExpectedTables =
    [
        "__EFMigrationsHistory",
        "categories",
        "transactions",
        "wallets"
    ];

    private static readonly string[] ExpectedColumns =
    [
        "categories|created_at|timestamp with time zone|required",
        "categories|id|uuid|required",
        "categories|name|character varying(100)|required",
        "categories|type|smallint|required",
        "transactions|amount|numeric(19,4)|required",
        "transactions|category_id|uuid|required",
        "transactions|created_at|timestamp with time zone|required",
        "transactions|currency_code|character varying(3)|required",
        "transactions|description|character varying(500)|nullable",
        "transactions|id|uuid|required",
        "transactions|occurred_at|timestamp with time zone|required",
        "transactions|type|smallint|required",
        "transactions|updated_at|timestamp with time zone|nullable",
        "transactions|wallet_id|uuid|required",
        "wallets|created_at|timestamp with time zone|required",
        "wallets|currency_code|character varying(3)|required",
        "wallets|id|uuid|required",
        "wallets|name|character varying(100)|required",
        "wallets|type|smallint|required"
    ];

    private static readonly string[] ExpectedConstraints =
    [
        "categories|ck_categories_type_defined",
        "categories|pk_categories",
        "transactions|ck_transactions_amount_positive",
        "transactions|ck_transactions_currency_code_format",
        "transactions|ck_transactions_type_defined",
        "transactions|fk_transactions_categories_category_id",
        "transactions|fk_transactions_wallets_wallet_id",
        "transactions|pk_transactions",
        "wallets|ck_wallets_currency_code_format",
        "wallets|ck_wallets_type_defined",
        "wallets|pk_wallets"
    ];

    private static readonly string[] ExpectedForeignKeys =
    [
        "fk_transactions_categories_category_id|categories|RESTRICT",
        "fk_transactions_wallets_wallet_id|wallets|RESTRICT"
    ];

    private static readonly string[] ExpectedIndexes =
    [
        "transactions|ix_transactions_category_id",
        "transactions|ix_transactions_occurred_at",
        "transactions|ix_transactions_wallet_id"
    ];

    [Fact]
    public void Model_ShouldMatchInitialMigration()
    {
        var factory = new SpendlyDbContextFactory();

        using var context = factory.CreateDbContext([]);

        var migration = Assert.Single(context.Database.GetMigrations());

        Assert.EndsWith(
            "_InitialCreate",
            migration,
            StringComparison.Ordinal);

        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task InitialMigration_ShouldApplyRollbackAndReapply()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var postgreSql =
            new PostgreSqlBuilder(PostgreSqlImage)
                .WithDatabase("spendly_migrations")
                .WithUsername("spendly")
                .WithPassword("spendly_password")
                .Build();

        await postgreSql.StartAsync(cancellationToken);

        var connectionString = postgreSql.GetConnectionString();
        var options = CreateOptions(connectionString);

        await ApplyMigrationsAsync(
            options,
            cancellationToken);

        await AssertMigrationStateAsync(
            options,
            expectedAppliedMigrationCount: 1,
            expectedPendingMigrationCount: 0,
            cancellationToken);

        await AssertSchemaAsync(
            connectionString,
            cancellationToken);

        var persistedIds = await AssertRoundTripAsync(
            options,
            cancellationToken);

        await AssertRestrictiveDeleteBehaviorAsync(
            options,
            persistedIds,
            cancellationToken);

        await RollBackAllMigrationsAsync(
            options,
            cancellationToken);

        await AssertMigrationStateAsync(
            options,
            expectedAppliedMigrationCount: 0,
            expectedPendingMigrationCount: 1,
            cancellationToken);

        await AssertDomainTablesDoNotExistAsync(
            connectionString,
            cancellationToken);

        await ApplyMigrationsAsync(
            options,
            cancellationToken);

        await AssertMigrationStateAsync(
            options,
            expectedAppliedMigrationCount: 1,
            expectedPendingMigrationCount: 0,
            cancellationToken);

        await AssertSchemaAsync(
            connectionString,
            cancellationToken);
    }

    private static DbContextOptions<SpendlyDbContext> CreateOptions(
        string connectionString)
    {
        return new DbContextOptionsBuilder<SpendlyDbContext>()
            .UseNpgsql(connectionString)
            .EnableDetailedErrors()
            .Options;
    }

    private static async Task ApplyMigrationsAsync(
        DbContextOptions<SpendlyDbContext> options,
        CancellationToken cancellationToken)
    {
        await using var context = new SpendlyDbContext(options);

        await context.Database.MigrateAsync(cancellationToken);
    }

    private static async Task RollBackAllMigrationsAsync(
        DbContextOptions<SpendlyDbContext> options,
        CancellationToken cancellationToken)
    {
        await using var context = new SpendlyDbContext(options);

        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync(
            Migration.InitialDatabase,
            cancellationToken);
    }

    private static async Task AssertMigrationStateAsync(
        DbContextOptions<SpendlyDbContext> options,
        int expectedAppliedMigrationCount,
        int expectedPendingMigrationCount,
        CancellationToken cancellationToken)
    {
        await using var context = new SpendlyDbContext(options);

        var knownMigration = Assert.Single(
            context.Database.GetMigrations());

        var appliedMigrations = (await context.Database
                .GetAppliedMigrationsAsync(cancellationToken))
            .ToArray();

        var pendingMigrations = (await context.Database
                .GetPendingMigrationsAsync(cancellationToken))
            .ToArray();

        Assert.False(context.Database.HasPendingModelChanges());

        Assert.Equal(
            expectedAppliedMigrationCount,
            appliedMigrations.Length);

        Assert.Equal(
            expectedPendingMigrationCount,
            pendingMigrations.Length);

        if (expectedAppliedMigrationCount is 1)
        {
            Assert.Equal(
                knownMigration,
                Assert.Single(appliedMigrations));
        }

        if (expectedPendingMigrationCount is 1)
        {
            Assert.Equal(
                knownMigration,
                Assert.Single(pendingMigrations));
        }
    }

    private static async Task AssertSchemaAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        await using var dataSource =
            NpgsqlDataSource.Create(connectionString);

        var actualTables = await ReadStringsAsync(
            dataSource,
            GetTablesSql,
            cancellationToken);

        var actualColumns = await ReadStringsAsync(
            dataSource,
            GetColumnsSql,
            cancellationToken);

        var actualConstraints = await ReadStringsAsync(
            dataSource,
            GetConstraintsSql,
            cancellationToken);

        var actualForeignKeys = await ReadStringsAsync(
            dataSource,
            GetForeignKeysSql,
            cancellationToken);

        var actualIndexes = await ReadStringsAsync(
            dataSource,
            GetIndexesSql,
            cancellationToken);

        Assert.Equal(ExpectedTables, actualTables);
        Assert.Equal(ExpectedColumns, actualColumns);
        Assert.Equal(ExpectedConstraints, actualConstraints);
        Assert.Equal(ExpectedForeignKeys, actualForeignKeys);
        Assert.Equal(ExpectedIndexes, actualIndexes);
    }

    private static async Task AssertDomainTablesDoNotExistAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        await using var dataSource =
            NpgsqlDataSource.Create(connectionString);

        var domainTables = await ReadStringsAsync(
            dataSource,
            GetDomainTablesSql,
            cancellationToken);

        Assert.Empty(domainTables);
    }

    private static async Task<string[]> ReadStringsAsync(
        NpgsqlDataSource dataSource,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(sql);

        await using var reader = await command.ExecuteReaderAsync(
            cancellationToken);

        var values = new List<string>();

        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(reader.GetString(0));
        }

        return values.ToArray();
    }

    private static async Task<PersistedIds> AssertRoundTripAsync(
        DbContextOptions<SpendlyDbContext> options,
        CancellationToken cancellationToken)
    {
        var createdAt = new DateTimeOffset(
            2026,
            7,
            27,
            12,
            0,
            0,
            TimeSpan.FromHours(3));

        var wallet = Wallet.Create(
            name: "  Main wallet  ",
            type: WalletType.DebitCard,
            currency: Currency.Rub,
            createdAt: createdAt);

        var category = Category.Create(
            name: "  Groceries  ",
            type: CategoryType.Expense,
            createdAt: createdAt.AddMinutes(1));

        var transaction = Transaction.Create(
            type: TransactionType.Expense,
            amount: Money.Positive(
                12_345.6700m,
                Currency.Rub),
            wallet: wallet,
            category: category,
            occurredAt: createdAt.AddHours(1),
            description: "  Groceries  ",
            createdAt: createdAt.AddHours(2));

        await using (var writeContext =
                     new SpendlyDbContext(options))
        {
            writeContext.Wallets.Add(wallet);
            writeContext.Categories.Add(category);
            writeContext.Transactions.Add(transaction);

            await writeContext.SaveChangesAsync(cancellationToken);
        }

        await using var readContext =
            new SpendlyDbContext(options);

        var actualWallet = await readContext.Wallets
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Id == wallet.Id,
                cancellationToken);

        var actualCategory = await readContext.Categories
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Id == category.Id,
                cancellationToken);

        var actualTransaction = await readContext.Transactions
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Id == transaction.Id,
                cancellationToken);

        Assert.NotSame(wallet, actualWallet);
        Assert.Equal(wallet.Id, actualWallet.Id);
        Assert.Equal("Main wallet", actualWallet.Name);
        Assert.Equal(wallet.Type, actualWallet.Type);
        Assert.Equal(wallet.Currency, actualWallet.Currency);
        Assert.Equal(TimeSpan.Zero, actualWallet.CreatedAt.Offset);

        Assert.NotSame(category, actualCategory);
        Assert.Equal(category.Id, actualCategory.Id);
        Assert.Equal("Groceries", actualCategory.Name);
        Assert.Equal(category.Type, actualCategory.Type);
        Assert.Equal(TimeSpan.Zero, actualCategory.CreatedAt.Offset);

        Assert.NotSame(transaction, actualTransaction);
        Assert.Equal(transaction.Id, actualTransaction.Id);
        Assert.Equal(transaction.Type, actualTransaction.Type);
        Assert.Equal(
            transaction.Amount,
            actualTransaction.Amount);
        Assert.Equal(
            transaction.WalletId,
            actualTransaction.WalletId);
        Assert.Equal(
            transaction.CategoryId,
            actualTransaction.CategoryId);
        Assert.Equal(
            transaction.OccurredAt,
            actualTransaction.OccurredAt);
        Assert.Equal(
            "Groceries",
            actualTransaction.Description);
        Assert.Equal(
            transaction.CreatedAt,
            actualTransaction.CreatedAt);
        Assert.Null(actualTransaction.UpdatedAt);
        Assert.Equal(
            TimeSpan.Zero,
            actualTransaction.OccurredAt.Offset);
        Assert.Equal(
            TimeSpan.Zero,
            actualTransaction.CreatedAt.Offset);

        Assert.Empty(readContext.ChangeTracker.Entries());

        return new PersistedIds(
            wallet.Id,
            category.Id);
    }

    private static async Task AssertRestrictiveDeleteBehaviorAsync(
        DbContextOptions<SpendlyDbContext> options,
        PersistedIds persistedIds,
        CancellationToken cancellationToken)
    {
        await using (var walletContext =
                     new SpendlyDbContext(options))
        {
            var wallet = await walletContext.Wallets.SingleAsync(
                candidate =>
                    candidate.Id == persistedIds.WalletId,
                cancellationToken);

            walletContext.Wallets.Remove(wallet);

            await Assert.ThrowsAsync<DbUpdateException>(
                () => walletContext.SaveChangesAsync(
                    cancellationToken));
        }

        await using var categoryContext =
            new SpendlyDbContext(options);

        var category = await categoryContext.Categories.SingleAsync(
            candidate =>
                candidate.Id == persistedIds.CategoryId,
            cancellationToken);

        categoryContext.Categories.Remove(category);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => categoryContext.SaveChangesAsync(
                cancellationToken));
    }

    private readonly record struct PersistedIds(
        WalletId WalletId,
        CategoryId CategoryId);

    private const string GetTablesSql =
        """
        SELECT tablename
        FROM pg_catalog.pg_tables
        WHERE schemaname = 'public'
        ORDER BY tablename COLLATE "C";
        """;

    private const string GetDomainTablesSql =
        """
        SELECT tablename
        FROM pg_catalog.pg_tables
        WHERE schemaname = 'public'
          AND tablename IN (
              'wallets',
              'categories',
              'transactions')
        ORDER BY tablename COLLATE "C";
        """;

    private const string GetColumnsSql =
        """
        SELECT format(
            '%s|%s|%s|%s',
            relation.relname,
            attribute.attname,
            pg_catalog.format_type(
                attribute.atttypid,
                attribute.atttypmod),
            CASE
                WHEN attribute.attnotnull THEN 'required'
                ELSE 'nullable'
            END)
        FROM pg_catalog.pg_attribute AS attribute
        INNER JOIN pg_catalog.pg_class AS relation
            ON relation.oid = attribute.attrelid
        INNER JOIN pg_catalog.pg_namespace AS namespace
            ON namespace.oid = relation.relnamespace
        WHERE namespace.nspname = 'public'
          AND relation.relname IN (
              'wallets',
              'categories',
              'transactions')
          AND relation.relkind IN ('r', 'p')
          AND attribute.attnum > 0
          AND NOT attribute.attisdropped
        ORDER BY
            relation.relname COLLATE "C",
            attribute.attname COLLATE "C";
        """;

    private const string GetConstraintsSql =
        """
        SELECT contract_item
        FROM
        (
            SELECT format(
                '%s|%s',
                relation.relname,
                constraint_definition.conname) AS contract_item
            FROM pg_catalog.pg_constraint AS constraint_definition
            INNER JOIN pg_catalog.pg_class AS relation
                ON relation.oid =
                    constraint_definition.conrelid
            INNER JOIN pg_catalog.pg_namespace AS namespace
                ON namespace.oid = relation.relnamespace
            WHERE namespace.nspname = 'public'
              AND relation.relname IN (
                  'wallets',
                  'categories',
                  'transactions')
              AND constraint_definition.contype IN ('c', 'f', 'p')
        ) AS constraint_contract
        ORDER BY contract_item COLLATE "C";
        """;

    private const string GetForeignKeysSql =
        """
        SELECT contract_item
        FROM
        (
            SELECT format(
                '%s|%s|%s',
                constraint_definition.conname,
                principal_relation.relname,
                CASE constraint_definition.confdeltype
                    WHEN 'a' THEN 'NO ACTION'
                    WHEN 'r' THEN 'RESTRICT'
                    WHEN 'c' THEN 'CASCADE'
                    WHEN 'n' THEN 'SET NULL'
                    WHEN 'd' THEN 'SET DEFAULT'
                END) AS contract_item
            FROM pg_catalog.pg_constraint AS constraint_definition
            INNER JOIN pg_catalog.pg_class AS dependent_relation
                ON dependent_relation.oid =
                    constraint_definition.conrelid
            INNER JOIN pg_catalog.pg_class AS principal_relation
                ON principal_relation.oid =
                    constraint_definition.confrelid
            INNER JOIN pg_catalog.pg_namespace AS namespace
                ON namespace.oid =
                    dependent_relation.relnamespace
            WHERE namespace.nspname = 'public'
              AND dependent_relation.relname = 'transactions'
              AND constraint_definition.contype = 'f'
        ) AS foreign_key_contract
        ORDER BY contract_item COLLATE "C";
        """;

    private const string GetIndexesSql =
        """
        SELECT contract_item
        FROM
        (
            SELECT format(
                '%s|%s',
                tablename,
                indexname) AS contract_item
            FROM pg_catalog.pg_indexes
            WHERE schemaname = 'public'
              AND tablename IN (
                  'wallets',
                  'categories',
                  'transactions')
              AND indexname LIKE 'ix_%'
        ) AS index_contract
        ORDER BY contract_item COLLATE "C";
        """;
}
