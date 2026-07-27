namespace Spendly.IntegrationTests.Database;

[CollectionDefinition(DisableParallelization = true)]
public sealed class PostgreSqlDatabaseCollection
    : ICollectionFixture<PostgreSqlDatabaseFixture>
{
}
