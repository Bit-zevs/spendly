using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Spendly.Domain.ValueObjects;
using Spendly.Domain.Wallets;
using Spendly.IntegrationTests.Database;

namespace Spendly.IntegrationTests.Persistence;

[Collection<PostgreSqlDatabaseCollection>]
public sealed class WalletPersistenceTests(
    PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database)
{
    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task Wallet_ShouldRoundTripWithoutDataLoss()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var sourceCreatedAt = CreateSourceCreatedAt();

        var wallet = Wallet.Create(
            name: "  Main wallet  ",
            type: WalletType.DebitCard,
            currency: Currency.From("KZT"),
            createdAt: sourceCreatedAt);

        AssertWalletPersistenceShape();
        Assert.Equal(7, wallet.Id.Value.Version);
        Assert.Equal(sourceCreatedAt.ToUniversalTime(), wallet.CreatedAt);

        await using (var writeContext = Database.CreateDbContext())
        {
            writeContext.Wallets.Add(wallet);

            await writeContext.SaveChangesAsync(cancellationToken);
        }

        await using var readContext = Database.CreateDbContext();

        var restoredWallet = await readContext.Wallets
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Id == wallet.Id,
                cancellationToken);

        Assert.NotSame(wallet, restoredWallet);
        Assert.Equal(wallet.Id, restoredWallet.Id);
        Assert.Equal(7, restoredWallet.Id.Value.Version);
        Assert.Equal("Main wallet", restoredWallet.Name);
        Assert.Equal(wallet.Type, restoredWallet.Type);
        Assert.Equal(wallet.Currency, restoredWallet.Currency);
        Assert.Equal(wallet.CreatedAt, restoredWallet.CreatedAt);
        Assert.Equal(TimeSpan.Zero, restoredWallet.CreatedAt.Offset);
        Assert.Empty(readContext.ChangeTracker.Entries());

        await AssertStoredAsPostgreSqlUuidAsync(
            wallet.Id,
            cancellationToken);
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task EveryDefinedWalletType_ShouldRoundTrip()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var walletTypes = Enum.GetValues<WalletType>();

        var wallets = walletTypes
            .Select(
                (walletType, index) => Wallet.Create(
                    name: $"Wallet {walletType}",
                    type: walletType,
                    currency: Currency.Rub,
                    createdAt: CreateUtcCreatedAt().AddMinutes(index)))
            .ToArray();

        await using (var writeContext = Database.CreateDbContext())
        {
            writeContext.Wallets.AddRange(wallets);

            await writeContext.SaveChangesAsync(cancellationToken);
        }

        await using var readContext = Database.CreateDbContext();

        var restoredWallets = await readContext.Wallets
            .AsNoTracking()
            .ToDictionaryAsync(
                wallet => wallet.Id,
                cancellationToken);

        Assert.Equal(wallets.Length, restoredWallets.Count);

        foreach (var wallet in wallets)
        {
            var restoredWallet = restoredWallets[wallet.Id];

            Assert.Equal(wallet.Type, restoredWallet.Type);
        }

        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task WalletName_AtMaximumLength_ShouldRoundTrip()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var maximumLengthName = new string('W', Wallet.MaxNameLength);

        var wallet = Wallet.Create(
            name: maximumLengthName,
            type: WalletType.Savings,
            currency: Currency.Usd,
            createdAt: CreateUtcCreatedAt());

        await using (var writeContext = Database.CreateDbContext())
        {
            writeContext.Wallets.Add(wallet);

            await writeContext.SaveChangesAsync(cancellationToken);
        }

        await using var readContext = Database.CreateDbContext();

        var restoredName = await readContext.Wallets
            .AsNoTracking()
            .Where(candidate => candidate.Id == wallet.Id)
            .Select(candidate => candidate.Name)
            .SingleAsync(cancellationToken);

        Assert.Equal(maximumLengthName, restoredName);
        Assert.Equal(Wallet.MaxNameLength, restoredName.Length);
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task WalletName_ExceedingMaximumLength_ShouldBeRejectedByPostgreSql()
    {
        var exception = await AssertInvalidWalletIsRejectedAsync(
            name: new string('W', Wallet.MaxNameLength + 1),
            type: (short)WalletType.Cash,
            currencyCode: Currency.Rub.Code);

        Assert.Equal(
            PostgresErrorCodes.StringDataRightTruncation,
            exception.SqlState);
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task UndefinedWalletType_ShouldBeRejectedByPostgreSql()
    {
        var exception = await AssertInvalidWalletIsRejectedAsync(
            name: "Invalid type wallet",
            type: 0,
            currencyCode: Currency.Rub.Code);

        Assert.Equal(
            PostgresErrorCodes.CheckViolation,
            exception.SqlState);

        Assert.Equal(
            "ck_wallets_type_defined",
            exception.ConstraintName);
    }

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task InvalidCurrencyCode_ShouldBeRejectedByPostgreSql()
    {
        var exception = await AssertInvalidWalletIsRejectedAsync(
            name: "Invalid currency wallet",
            type: (short)WalletType.Cash,
            currencyCode: "rub");

        Assert.Equal(
            PostgresErrorCodes.CheckViolation,
            exception.SqlState);

        Assert.Equal(
            "ck_wallets_currency_code_format",
            exception.ConstraintName);
    }

    private async Task<PostgresException> AssertInvalidWalletIsRejectedAsync(
        string name,
        short type,
        string currencyCode)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var context = Database.CreateDbContext();

        return await Assert.ThrowsAsync<PostgresException>(
            () => context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO wallets (
                    id,
                    name,
                    type,
                    currency_code,
                    created_at)
                VALUES (
                    {Guid.CreateVersion7()},
                    {name},
                    {type},
                    {currencyCode},
                    {CreateUtcCreatedAt()});
                """,
                cancellationToken));
    }

    private async Task AssertStoredAsPostgreSqlUuidAsync(
        WalletId walletId,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT id, pg_typeof(id)::text
            FROM wallets
            WHERE id = @wallet_id;
            """;

        await using var dataSource =
            NpgsqlDataSource.Create(Database.ConnectionString);

        await using var command = dataSource.CreateCommand(sql);

        command.Parameters.AddWithValue(
            "wallet_id",
            walletId.Value);

        await using var reader = await command.ExecuteReaderAsync(
            cancellationToken);

        Assert.True(await reader.ReadAsync(cancellationToken));
        Assert.Equal(walletId.Value, reader.GetGuid(0));
        Assert.Equal("uuid", reader.GetString(1));
        Assert.False(await reader.ReadAsync(cancellationToken));
    }

    private static void AssertWalletPersistenceShape()
    {
        string[] propertyNames =
        [
            nameof(Wallet.Id),
            nameof(Wallet.Name),
            nameof(Wallet.Type),
            nameof(Wallet.Currency),
            nameof(Wallet.CreatedAt)
        ];

        foreach (var propertyName in propertyNames)
        {
            var property = typeof(Wallet).GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);

            Assert.NotNull(property);
            Assert.False(
                property.SetMethod?.IsPublic is true,
                $"{nameof(Wallet)}.{propertyName} must not expose a public setter.");
        }

        var constructor = typeof(Wallet).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types:
            [
                typeof(WalletId),
                typeof(string),
                typeof(WalletType),
                typeof(Currency),
                typeof(DateTimeOffset)
            ],
            modifiers: null);

        Assert.NotNull(constructor);
        Assert.True(constructor.IsPrivate);
    }

    private static DateTimeOffset CreateSourceCreatedAt()
    {
        return new DateTimeOffset(
            2026,
            7,
            27,
            12,
            34,
            56,
            789,
            TimeSpan.FromHours(5));
    }

    private static DateTimeOffset CreateUtcCreatedAt()
    {
        return CreateSourceCreatedAt().ToUniversalTime();
    }
}
