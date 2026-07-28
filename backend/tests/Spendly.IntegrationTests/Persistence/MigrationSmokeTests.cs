using Microsoft.EntityFrameworkCore;
using Npgsql;
using Spendly.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Spendly.IntegrationTests.Persistence;

public sealed class MigrationSmokeTests
{
    private const string PostgreSqlImage = "postgres:17.10";

    private static readonly string[] ExpectedTables =
    [
        "__EFMigrationsHistory",
        "categories",
        "transactions",
        "wallets"
    ];

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task AllMigrations_ShouldBuildUsableSchemaFromEmptyDatabase()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var postgreSql = new PostgreSqlBuilder(PostgreSqlImage)
            .WithDatabase("spendly_migration_smoke")
            .WithUsername("spendly")
            .WithPassword("spendly_password")
            .Build();

        await postgreSql.StartAsync(cancellationToken);

        await using var dataSource = NpgsqlDataSource.Create(
            postgreSql.GetConnectionString());

        var options = new DbContextOptionsBuilder<SpendlyDbContext>()
            .UseNpgsql(dataSource)
            .EnableDetailedErrors()
            .Options;

        var tablesBeforeMigration = await GetPublicTableNamesAsync(
            dataSource,
            cancellationToken);

        Assert.Empty(tablesBeforeMigration);

        string[] knownMigrations;

        await using (var migrationContext = new SpendlyDbContext(options))
        {
            knownMigrations = migrationContext.Database
                .GetMigrations()
                .ToArray();

            var appliedMigrationsBeforeMigration =
                (await migrationContext.Database.GetAppliedMigrationsAsync(
                    cancellationToken))
                .ToArray();

            var pendingMigrationsBeforeMigration =
                (await migrationContext.Database.GetPendingMigrationsAsync(
                    cancellationToken))
                .ToArray();

            Assert.NotEmpty(knownMigrations);
            Assert.Empty(appliedMigrationsBeforeMigration);
            Assert.Equal(
                knownMigrations,
                pendingMigrationsBeforeMigration);

            await migrationContext.Database.MigrateAsync(cancellationToken);
        }

        var tablesAfterMigration = await GetPublicTableNamesAsync(
            dataSource,
            cancellationToken);

        Assert.Equal(ExpectedTables, tablesAfterMigration);

        await using var verificationContext =
            new SpendlyDbContext(options);

        var appliedMigrations =
            (await verificationContext.Database.GetAppliedMigrationsAsync(
                cancellationToken))
            .ToArray();

        var pendingMigrations =
            (await verificationContext.Database.GetPendingMigrationsAsync(
                cancellationToken))
            .ToArray();

        Assert.Equal(knownMigrations, appliedMigrations);
        Assert.Empty(pendingMigrations);
        Assert.True(
            await verificationContext.Database.CanConnectAsync(
                cancellationToken));

        Assert.False(
            await verificationContext.Wallets.AnyAsync(cancellationToken));
        Assert.False(
            await verificationContext.Categories.AnyAsync(cancellationToken));
        Assert.False(
            await verificationContext.Transactions.AnyAsync(
                cancellationToken));
    }

    private static async Task<string[]> GetPublicTableNamesAsync(
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT tablename
            FROM pg_catalog.pg_tables
            WHERE schemaname = 'public'
            ORDER BY tablename COLLATE "C";
            """);

        await using var reader = await command.ExecuteReaderAsync(
            cancellationToken);

        var tableNames = new List<string>();

        while (await reader.ReadAsync(cancellationToken))
        {
            tableNames.Add(reader.GetString(0));
        }

        return tableNames.ToArray();
    }
}
