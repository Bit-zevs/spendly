namespace Spendly.IntegrationTests.Database;

public abstract class DatabaseIntegrationTest(
    PostgreSqlDatabaseFixture database)
    : IAsyncLifetime
{
    protected PostgreSqlDatabaseFixture Database { get; } =
        database ?? throw new ArgumentNullException(nameof(database));

    public async ValueTask InitializeAsync()
    {
        await Database.ResetDatabaseAsync(
            TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
