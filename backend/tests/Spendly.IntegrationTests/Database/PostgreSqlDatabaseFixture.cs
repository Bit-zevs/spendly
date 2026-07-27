using Microsoft.EntityFrameworkCore;
using Npgsql;
using Spendly.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Spendly.IntegrationTests.Database;

public sealed class PostgreSqlDatabaseFixture : IAsyncLifetime
{
    private const string PostgreSqlImage = "postgres:17.10";
    private const string DatabaseName = "spendly_tests";
    private const string Username = "spendly";
    private const string Password = "spendly_password";

    private const string DockerStartErrorMessage =
        "Unable to start the PostgreSQL Testcontainer. "
        + "Database integration tests require a running "
        + "Docker-compatible container engine. Ensure Docker Desktop "
        + "or another compatible engine is installed and running, "
        + "and that the pinned PostgreSQL image can be pulled.";

    private const string ResetDatabaseSql =
        """
        DO $reset$
        DECLARE
            tables_to_truncate text;
        BEGIN
            SELECT string_agg(
                format('%I.%I', schemaname, tablename),
                ', ')
            INTO tables_to_truncate
            FROM pg_catalog.pg_tables
            WHERE schemaname = 'public'
              AND tablename <> '__EFMigrationsHistory';

            IF tables_to_truncate IS NOT NULL THEN
                EXECUTE
                    'TRUNCATE TABLE '
                    || tables_to_truncate
                    || ' RESTART IDENTITY CASCADE';
            END IF;
        END;
        $reset$;
        """;

    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder(PostgreSqlImage)
            .WithDatabase(DatabaseName)
            .WithUsername(Username)
            .WithPassword(Password)
            .Build();

    private NpgsqlDataSource? _dataSource;
    private DbContextOptions<SpendlyDbContext>? _dbContextOptions;
    private string? _connectionString;
    private int _disposeState;

    public string ConnectionString =>
        _connectionString
        ?? throw new InvalidOperationException(
            "The PostgreSQL database fixture has not been initialized.");

    public async ValueTask InitializeAsync()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        try
        {
            await _container.StartAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await DisposeAfterInitializationFailureAsync();
            throw;
        }
        catch (Exception exception)
        {
            await DisposeAfterInitializationFailureAsync();

            throw new InvalidOperationException(
                DockerStartErrorMessage,
                exception);
        }

        try
        {
            _connectionString = _container.GetConnectionString();
            _dataSource = NpgsqlDataSource.Create(_connectionString);

            _dbContextOptions =
                new DbContextOptionsBuilder<SpendlyDbContext>()
                    .UseNpgsql(_dataSource)
                    .EnableDetailedErrors()
                    .Options;

            await ApplyMigrationsAsync(cancellationToken);
        }
        catch
        {
            await DisposeAfterInitializationFailureAsync();
            throw;
        }
    }

    public SpendlyDbContext CreateDbContext()
    {
        ObjectDisposedException.ThrowIf(
            Volatile.Read(ref _disposeState) is not 0,
            this);

        var options = _dbContextOptions
            ?? throw new InvalidOperationException(
                "The PostgreSQL database fixture has not been initialized.");

        return new SpendlyDbContext(options);
    }

    public async Task ResetDatabaseAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(
            Volatile.Read(ref _disposeState) is not 0,
            this);

        await ApplyMigrationsAsync(cancellationToken);

        var dataSource = _dataSource
            ?? throw new InvalidOperationException(
                "The PostgreSQL database fixture has not been initialized.");

        await using var command =
            dataSource.CreateCommand(ResetDatabaseSql);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) is not 0)
        {
            return;
        }

        try
        {
            if (_dataSource is not null)
            {
                await _dataSource.DisposeAsync();
            }
        }
        finally
        {
            await _container.DisposeAsync();
        }
    }

    private async Task ApplyMigrationsAsync(
        CancellationToken cancellationToken)
    {
        await using var context = CreateDbContext();

        await context.Database.MigrateAsync(cancellationToken);
    }

    private async ValueTask DisposeAfterInitializationFailureAsync()
    {
        try
        {
            await DisposeAsync();
        }
        catch
        {
            // Preserve the initialization exception as the primary failure.
        }
    }
}
