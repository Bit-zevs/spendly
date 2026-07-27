using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Spendly.Domain.Categories;
using Spendly.Domain.Transactions;
using Spendly.Domain.ValueObjects;
using Spendly.Domain.Wallets;
using Spendly.IntegrationTests.Database;

namespace Spendly.IntegrationTests.Persistence;

[Collection<PostgreSqlDatabaseCollection>]
public sealed class TransactionPersistenceTests(
    PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database)
{
    private static readonly decimal[] ExactAmounts =
    [
        0.0001m,
        12_345.6789m,
        Money.MaxAmount
    ];

    private static readonly string[] ExpectedIndexes =
    [
        "ix_transactions_category_id|category_id",
        "ix_transactions_occurred_at|occurred_at",
        "ix_transactions_wallet_id|wallet_id"
    ];

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task Transaction_ShouldRoundTripWithoutDataLoss()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var sourceOccurredAt = CreateSourceOccurredAt();
        var sourceCreatedAt = CreateSourceCreatedAt();

        var wallet = Wallet.Create(
            name: "Main wallet",
            type: WalletType.DebitCard,
            currency: Currency.Rub,
            createdAt: sourceCreatedAt.AddMinutes(-2));

        var category = Category.Create(
            name: "Groceries",
            type: CategoryType.Expense,
            createdAt: sourceCreatedAt.AddMinutes(-1));

        var transaction = Transaction.Create(
            type: TransactionType.Expense,
            amount: Money.Positive(12_345.6789m, Currency.Rub),
            wallet: wallet,
            category: category,
            occurredAt: sourceOccurredAt,
            description: "  Weekly groceries  ",
            createdAt: sourceCreatedAt);

        Assert.Equal(7, transaction.Id.Value.Version);
        Assert.Equal(sourceOccurredAt.ToUniversalTime(), transaction.OccurredAt);
        Assert.Equal(sourceCreatedAt.ToUniversalTime(), transaction.CreatedAt);
        Assert.Null(transaction.UpdatedAt);

        await using (var writeContext = Database.CreateDbContext())
        {
            writeContext.Wallets.Add(wallet);
            writeContext.Categories.Add(category);
            writeContext.Transactions.Add(transaction);

            await writeContext.SaveChangesAsync(cancellationToken);
        }

        await using var readContext = Database.CreateDbContext();

        var restoredWallet = await readContext.Wallets
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Id == wallet.Id,
                cancellationToken);

        var restoredCategory = await readContext.Categories
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Id == category.Id,
                cancellationToken);

        var restoredTransaction = await readContext.Transactions
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Id == transaction.Id,
                cancellationToken);

        Assert.NotSame(wallet, restoredWallet);
        Assert.Equal(wallet.Id, restoredWallet.Id);

        Assert.NotSame(category, restoredCategory);
        Assert.Equal(category.Id, restoredCategory.Id);

        Assert.NotSame(transaction, restoredTransaction);
        Assert.Equal(transaction.Id, restoredTransaction.Id);
        Assert.Equal(7, restoredTransaction.Id.Value.Version);
        Assert.Equal(transaction.WalletId, restoredTransaction.WalletId);
        Assert.Equal(transaction.CategoryId, restoredTransaction.CategoryId);
        Assert.Equal(transaction.Type, restoredTransaction.Type);
        Assert.Equal(
            transaction.Amount.Amount,
            restoredTransaction.Amount.Amount);
        Assert.Equal(
            transaction.Amount.Currency,
            restoredTransaction.Amount.Currency);
        Assert.Equal(transaction.OccurredAt, restoredTransaction.OccurredAt);
        Assert.Equal(TimeSpan.Zero, restoredTransaction.OccurredAt.Offset);
        Assert.Equal("Weekly groceries", restoredTransaction.Description);
        Assert.Equal(transaction.CreatedAt, restoredTransaction.CreatedAt);
        Assert.Equal(TimeSpan.Zero, restoredTransaction.CreatedAt.Offset);
        Assert.Null(restoredTransaction.UpdatedAt);
        Assert.Empty(readContext.ChangeTracker.Entries());

        await AssertStoredUsingPostgreSqlContractAsync(
            transaction,
            cancellationToken);
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task EverySupportedTransactionType_ShouldRoundTrip()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var createdAt = CreateUtcCreatedAt();

        var wallet = Wallet.Create(
            name: "Universal wallet",
            type: WalletType.Cash,
            currency: Currency.Rub,
            createdAt: createdAt);

        var incomeCategory = Category.Create(
            name: "Salary",
            type: CategoryType.Income,
            createdAt: createdAt.AddMinutes(1));

        var expenseCategory = Category.Create(
            name: "Transport",
            type: CategoryType.Expense,
            createdAt: createdAt.AddMinutes(2));

        Transaction[] transactions =
        [
            Transaction.Create(
                type: TransactionType.Income,
                amount: Money.Positive(150_000m, Currency.Rub),
                wallet: wallet,
                category: incomeCategory,
                occurredAt: createdAt.AddHours(1),
                description: "Salary",
                createdAt: createdAt.AddHours(2)),
            Transaction.Create(
                type: TransactionType.Expense,
                amount: Money.Positive(250.50m, Currency.Rub),
                wallet: wallet,
                category: expenseCategory,
                occurredAt: createdAt.AddHours(3),
                description: "Taxi",
                createdAt: createdAt.AddHours(4))
        ];

        await using (var writeContext = Database.CreateDbContext())
        {
            writeContext.Wallets.Add(wallet);
            writeContext.Categories.AddRange(
                incomeCategory,
                expenseCategory);
            writeContext.Transactions.AddRange(transactions);

            await writeContext.SaveChangesAsync(cancellationToken);
        }

        await using var readContext = Database.CreateDbContext();

        var restoredTransactions = await readContext.Transactions
            .AsNoTracking()
            .ToDictionaryAsync(
                transaction => transaction.Id,
                cancellationToken);

        Assert.Equal(transactions.Length, restoredTransactions.Count);

        foreach (var transaction in transactions)
        {
            var restoredTransaction = restoredTransactions[transaction.Id];

            Assert.Equal(transaction.Type, restoredTransaction.Type);
            Assert.Equal(transaction.WalletId, restoredTransaction.WalletId);
            Assert.Equal(
                transaction.CategoryId,
                restoredTransaction.CategoryId);
        }

        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task MoneyAmounts_WithFourFractionalDigits_ShouldRoundTripExactly()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var createdAt = CreateUtcCreatedAt();

        var wallet = Wallet.Create(
            name: "Precision wallet",
            type: WalletType.Savings,
            currency: Currency.Rub,
            createdAt: createdAt);

        var category = Category.Create(
            name: "Precision checks",
            type: CategoryType.Expense,
            createdAt: createdAt.AddMinutes(1));

        var transactions = ExactAmounts
            .Select(
                (amount, index) => Transaction.Create(
                    type: TransactionType.Expense,
                    amount: Money.Positive(amount, Currency.Rub),
                    wallet: wallet,
                    category: category,
                    occurredAt: createdAt.AddMinutes(index + 2),
                    description: $"Exact amount {index + 1}",
                    createdAt: createdAt.AddMinutes(index + 5)))
            .ToArray();

        await using (var writeContext = Database.CreateDbContext())
        {
            writeContext.Wallets.Add(wallet);
            writeContext.Categories.Add(category);
            writeContext.Transactions.AddRange(transactions);

            await writeContext.SaveChangesAsync(cancellationToken);
        }

        await using var readContext = Database.CreateDbContext();

        var restoredTransactions = await readContext.Transactions
            .AsNoTracking()
            .ToDictionaryAsync(
                transaction => transaction.Id,
                cancellationToken);

        Assert.Equal(transactions.Length, restoredTransactions.Count);

        foreach (var transaction in transactions)
        {
            var restoredAmount = restoredTransactions[transaction.Id]
                .Amount
                .Amount;

            Assert.Equal(transaction.Amount.Amount, restoredAmount);
        }

        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task NullableDescription_ShouldRoundTripAsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (wallet, category) = CreateExpensePrincipals();

        var transaction = CreateExpenseTransaction(
            wallet,
            category,
            description: "   ");

        Assert.Null(transaction.Description);

        await PersistAsync(
            wallet,
            category,
            transaction,
            cancellationToken);

        await using var readContext = Database.CreateDbContext();

        var restoredDescription = await readContext.Transactions
            .AsNoTracking()
            .Where(candidate => candidate.Id == transaction.Id)
            .Select(candidate => candidate.Description)
            .SingleAsync(cancellationToken);

        Assert.Null(restoredDescription);
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task Description_AtMaximumLength_ShouldRoundTrip()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var maximumLengthDescription = new string(
            'D',
            Transaction.MaxDescriptionLength);

        var (wallet, category) = CreateExpensePrincipals();
        var transaction = CreateExpenseTransaction(
            wallet,
            category,
            maximumLengthDescription);

        await PersistAsync(
            wallet,
            category,
            transaction,
            cancellationToken);

        await using var readContext = Database.CreateDbContext();

        var restoredDescription = await readContext.Transactions
            .AsNoTracking()
            .Where(candidate => candidate.Id == transaction.Id)
            .Select(candidate => candidate.Description)
            .SingleAsync(cancellationToken);

        Assert.NotNull(restoredDescription);
        Assert.Equal(maximumLengthDescription, restoredDescription);
        Assert.Equal(
            Transaction.MaxDescriptionLength,
            restoredDescription.Length);
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task Description_ExceedingMaximumLength_ShouldBeRejectedByPostgreSql()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (wallet, category) = CreateExpensePrincipals();

        await PersistPrincipalsAsync(
            wallet,
            category,
            cancellationToken);

        var exception = await AssertTransactionInsertIsRejectedAsync(
            wallet.Id.Value,
            category.Id.Value,
            new string('D', Transaction.MaxDescriptionLength + 1),
            cancellationToken);

        Assert.Equal(
            PostgresErrorCodes.StringDataRightTruncation,
            exception.SqlState);
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task MissingWalletForeignKey_ShouldBeRejectedByPostgreSql()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (wallet, category) = CreateExpensePrincipals();

        await PersistPrincipalsAsync(
            wallet,
            category,
            cancellationToken);

        var exception = await AssertTransactionInsertIsRejectedAsync(
            Guid.CreateVersion7(),
            category.Id.Value,
            description: null,
            cancellationToken: cancellationToken);

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
        Assert.Equal(
            "fk_transactions_wallets_wallet_id",
            exception.ConstraintName);
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task MissingCategoryForeignKey_ShouldBeRejectedByPostgreSql()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (wallet, category) = CreateExpensePrincipals();

        await PersistPrincipalsAsync(
            wallet,
            category,
            cancellationToken);

        var exception = await AssertTransactionInsertIsRejectedAsync(
            wallet.Id.Value,
            Guid.CreateVersion7(),
            description: null,
            cancellationToken: cancellationToken);

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
        Assert.Equal(
            "fk_transactions_categories_category_id",
            exception.ConstraintName);
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task ReferencedWallet_ShouldNotBeDeleted()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (wallet, category) = CreateExpensePrincipals();
        var transaction = CreateExpenseTransaction(wallet, category);

        await PersistAsync(
            wallet,
            category,
            transaction,
            cancellationToken);

        var exception = await AssertDeleteIsRejectedAsync(
            tableName: "wallets",
            id: wallet.Id.Value,
            cancellationToken: cancellationToken);

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
        Assert.Equal(
            "fk_transactions_wallets_wallet_id",
            exception.ConstraintName);

        await AssertRowsStillExistAsync(
            wallet.Id,
            category.Id,
            transaction.Id,
            cancellationToken);
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task ReferencedCategory_ShouldNotBeDeleted()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (wallet, category) = CreateExpensePrincipals();
        var transaction = CreateExpenseTransaction(wallet, category);

        await PersistAsync(
            wallet,
            category,
            transaction,
            cancellationToken);

        var exception = await AssertDeleteIsRejectedAsync(
            tableName: "categories",
            id: category.Id.Value,
            cancellationToken: cancellationToken);

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
        Assert.Equal(
            "fk_transactions_categories_category_id",
            exception.ConstraintName);

        await AssertRowsStillExistAsync(
            wallet.Id,
            category.Id,
            transaction.Id,
            cancellationToken);
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task TransactionIndexes_ShouldExistForExpectedColumns()
    {
        const string sql =
            """
            SELECT format(
                '%s|%s',
                index_relation.relname,
                string_agg(
                    pg_catalog.pg_get_indexdef(
                        index_definition.indexrelid,
                        key_position.position,
                        true),
                    ','
                    ORDER BY key_position.position))
            FROM pg_catalog.pg_index AS index_definition
            INNER JOIN pg_catalog.pg_class AS table_relation
                ON table_relation.oid = index_definition.indrelid
            INNER JOIN pg_catalog.pg_class AS index_relation
                ON index_relation.oid = index_definition.indexrelid
            INNER JOIN pg_catalog.pg_namespace AS namespace
                ON namespace.oid = table_relation.relnamespace
            CROSS JOIN LATERAL generate_series(
                1,
                index_definition.indnkeyatts)
                AS key_position(position)
            WHERE namespace.nspname = 'public'
              AND table_relation.relname = 'transactions'
              AND index_relation.relname LIKE 'ix_transactions_%'
            GROUP BY
                index_relation.relname,
                index_definition.indexrelid
            ORDER BY index_relation.relname COLLATE "C";
            """;

        var cancellationToken = TestContext.Current.CancellationToken;

        await using var dataSource =
            NpgsqlDataSource.Create(Database.ConnectionString);

        await using var command = dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync(
            cancellationToken);

        var actualIndexes = new List<string>();

        while (await reader.ReadAsync(cancellationToken))
        {
            actualIndexes.Add(reader.GetString(0));
        }

        Assert.Equal(ExpectedIndexes, actualIndexes.ToArray());
    }

    private static (Wallet Wallet, Category Category) CreateExpensePrincipals()
    {
        var createdAt = CreateUtcCreatedAt();

        var wallet = Wallet.Create(
            name: "Main wallet",
            type: WalletType.DebitCard,
            currency: Currency.Rub,
            createdAt: createdAt);

        var category = Category.Create(
            name: "Groceries",
            type: CategoryType.Expense,
            createdAt: createdAt.AddMinutes(1));

        return (wallet, category);
    }

    private static Transaction CreateExpenseTransaction(
        Wallet wallet,
        Category category,
        string? description = "Groceries")
    {
        return Transaction.Create(
            type: TransactionType.Expense,
            amount: Money.Positive(1_234.5678m, Currency.Rub),
            wallet: wallet,
            category: category,
            occurredAt: CreateUtcOccurredAt(),
            description: description,
            createdAt: CreateUtcCreatedAt().AddHours(2));
    }

    private async Task PersistAsync(
        Wallet wallet,
        Category category,
        Transaction transaction,
        CancellationToken cancellationToken)
    {
        await using var context = Database.CreateDbContext();

        context.Wallets.Add(wallet);
        context.Categories.Add(category);
        context.Transactions.Add(transaction);

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task PersistPrincipalsAsync(
        Wallet wallet,
        Category category,
        CancellationToken cancellationToken)
    {
        await using var context = Database.CreateDbContext();

        context.Wallets.Add(wallet);
        context.Categories.Add(category);

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<PostgresException> AssertTransactionInsertIsRejectedAsync(
        Guid walletId,
        Guid categoryId,
        string? description,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            INSERT INTO transactions (
                id,
                type,
                wallet_id,
                category_id,
                occurred_at,
                description,
                created_at,
                updated_at,
                amount,
                currency_code)
            VALUES (
                @transaction_id,
                @type,
                @wallet_id,
                @category_id,
                @occurred_at,
                @description,
                @created_at,
                @updated_at,
                @amount,
                @currency_code);
            """;

        await using var dataSource =
            NpgsqlDataSource.Create(Database.ConnectionString);

        await using var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(
            "transaction_id",
            NpgsqlDbType.Uuid,
            Guid.CreateVersion7());

        command.Parameters.AddWithValue(
            "type",
            NpgsqlDbType.Smallint,
            (short)TransactionType.Expense);

        command.Parameters.AddWithValue(
            "wallet_id",
            NpgsqlDbType.Uuid,
            walletId);

        command.Parameters.AddWithValue(
            "category_id",
            NpgsqlDbType.Uuid,
            categoryId);

        command.Parameters.AddWithValue(
            "occurred_at",
            NpgsqlDbType.TimestampTz,
            CreateUtcOccurredAt());

        var descriptionParameter = command.Parameters.Add(
            "description",
            NpgsqlDbType.Varchar);

        descriptionParameter.Value = (object?)description ?? DBNull.Value;

        command.Parameters.AddWithValue(
            "created_at",
            NpgsqlDbType.TimestampTz,
            CreateUtcCreatedAt());

        var updatedAtParameter = command.Parameters.Add(
            "updated_at",
            NpgsqlDbType.TimestampTz);

        updatedAtParameter.Value = DBNull.Value;

        command.Parameters.AddWithValue(
            "amount",
            NpgsqlDbType.Numeric,
            1_234.5678m);

        command.Parameters.AddWithValue(
            "currency_code",
            NpgsqlDbType.Varchar,
            Currency.Rub.Code);

        return await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync(cancellationToken));
    }

    private async Task<PostgresException> AssertDeleteIsRejectedAsync(
        string tableName,
        Guid id,
        CancellationToken cancellationToken)
    {
        var sql = $"DELETE FROM {tableName} WHERE id = @id;";

        await using var dataSource =
            NpgsqlDataSource.Create(Database.ConnectionString);

        await using var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(
            "id",
            NpgsqlDbType.Uuid,
            id);

        return await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync(cancellationToken));
    }

    private async Task AssertRowsStillExistAsync(
        WalletId walletId,
        CategoryId categoryId,
        TransactionId transactionId,
        CancellationToken cancellationToken)
    {
        await using var context = Database.CreateDbContext();

        Assert.True(await context.Wallets
            .AsNoTracking()
            .AnyAsync(
                wallet => wallet.Id == walletId,
                cancellationToken));

        Assert.True(await context.Categories
            .AsNoTracking()
            .AnyAsync(
                category => category.Id == categoryId,
                cancellationToken));

        Assert.True(await context.Transactions
            .AsNoTracking()
            .AnyAsync(
                transaction => transaction.Id == transactionId,
                cancellationToken));
    }

    private async Task AssertStoredUsingPostgreSqlContractAsync(
        Transaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT
                id,
                pg_typeof(id)::text,
                type,
                pg_typeof(type)::text,
                wallet_id,
                pg_typeof(wallet_id)::text,
                category_id,
                pg_typeof(category_id)::text,
                amount,
                pg_typeof(amount)::text,
                currency_code,
                pg_typeof(currency_code)::text,
                occurred_at,
                pg_typeof(occurred_at)::text,
                description,
                created_at,
                pg_typeof(created_at)::text,
                updated_at,
                pg_typeof(updated_at)::text
            FROM transactions
            WHERE id = @transaction_id;
            """;

        await using var dataSource =
            NpgsqlDataSource.Create(Database.ConnectionString);

        await using var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(
            "transaction_id",
            NpgsqlDbType.Uuid,
            transaction.Id.Value);

        await using var reader = await command.ExecuteReaderAsync(
            cancellationToken);

        Assert.True(await reader.ReadAsync(cancellationToken));
        Assert.Equal(transaction.Id.Value, reader.GetGuid(0));
        Assert.Equal("uuid", reader.GetString(1));
        Assert.Equal((short)transaction.Type, reader.GetInt16(2));
        Assert.Equal("smallint", reader.GetString(3));
        Assert.Equal(transaction.WalletId.Value, reader.GetGuid(4));
        Assert.Equal("uuid", reader.GetString(5));
        Assert.Equal(transaction.CategoryId.Value, reader.GetGuid(6));
        Assert.Equal("uuid", reader.GetString(7));
        Assert.Equal(transaction.Amount.Amount, reader.GetDecimal(8));
        Assert.Equal("numeric", reader.GetString(9));
        Assert.Equal(transaction.Amount.Currency.Code, reader.GetString(10));
        Assert.Equal("character varying", reader.GetString(11));

        var storedOccurredAt = reader.GetFieldValue<DateTimeOffset>(12);

        Assert.Equal(transaction.OccurredAt, storedOccurredAt);
        Assert.Equal(TimeSpan.Zero, storedOccurredAt.Offset);
        Assert.Equal("timestamp with time zone", reader.GetString(13));
        Assert.Equal(transaction.Description, reader.GetString(14));

        var storedCreatedAt = reader.GetFieldValue<DateTimeOffset>(15);

        Assert.Equal(transaction.CreatedAt, storedCreatedAt);
        Assert.Equal(TimeSpan.Zero, storedCreatedAt.Offset);
        Assert.Equal("timestamp with time zone", reader.GetString(16));
        Assert.True(reader.IsDBNull(17));
        Assert.Equal("timestamp with time zone", reader.GetString(18));
        Assert.False(await reader.ReadAsync(cancellationToken));
    }

    private static DateTimeOffset CreateSourceOccurredAt()
    {
        return new DateTimeOffset(
            2026,
            7,
            27,
            18,
            45,
            30,
            123,
            TimeSpan.FromHours(5));
    }

    private static DateTimeOffset CreateSourceCreatedAt()
    {
        return new DateTimeOffset(
            2026,
            7,
            27,
            19,
            0,
            0,
            456,
            TimeSpan.FromHours(5));
    }

    private static DateTimeOffset CreateUtcOccurredAt()
    {
        return CreateSourceOccurredAt().ToUniversalTime();
    }

    private static DateTimeOffset CreateUtcCreatedAt()
    {
        return CreateSourceCreatedAt().ToUniversalTime();
    }
}
