# Spendly.IntegrationTests

Integration tests for Spendly API and infrastructure boundaries.

## API tests

API tests start the application in memory through
`WebApplicationFactory<Program>` and send HTTP requests through `HttpClient`.

Current API test scope:

- API host smoke tests;
- health check endpoint tests;
- ProblemDetails response tests;
- OpenAPI and Scalar availability tests;
- configuration and route-collision validation tests.

These tests do not use the database fixture and do not require Docker during a
normal test run.

## Persistence tests without Docker

Metadata-based persistence tests verify the production EF Core model without
opening a database connection.

They protect:

- strongly typed identifier converters;
- `Currency` conversion;
- `Money` complex-property mapping;
- PostgreSQL column types;
- enum mappings;
- explicit constraint and index names;
- restrictive delete behavior;
- consistency between the current model and migrations.

## PostgreSQL database tests

Database integration tests use:

- the production `SpendlyDbContext`;
- production EF Core migrations;
- Npgsql;
- PostgreSQL `17.10`;
- Testcontainers for .NET;
- an xUnit v3 collection fixture.

`PostgreSqlDatabaseFixture` performs the shared database lifecycle:

1. starts one temporary PostgreSQL container for the database-test collection;
2. builds `DbContextOptions<SpendlyDbContext>` from the container connection
   string;
3. applies production migrations through `MigrateAsync()`;
4. creates fresh `SpendlyDbContext` instances for tests;
5. reapplies missing migrations and truncates application tables before every
   database test while preserving `__EFMigrationsHistory`;
6. disposes the Npgsql data source and removes the container after the
   collection finishes.

All database test classes belong to `PostgreSqlDatabaseCollection` and inherit
from `DatabaseIntegrationTest`. Tests in this collection are serialized so that
a shared database reset cannot race with another database test.

The migration integration test additionally verifies:

- applied and pending migration state;
- the physical PostgreSQL schema;
- a real write/read round trip through separate contexts;
- restrictive foreign keys;
- rollback to the empty migration and successful reapplication.

The PostgreSQL readiness health-check test intentionally uses a separate empty
container. Its contract requires the health check to leave the schema unchanged,
so it does not use the migrated database fixture.

The accepted storage rules are documented in
[ADR 0003](../../../docs/adr/0003-define-domain-model-persistence-strategy.md).

## Docker behavior

Database tests and the live PostgreSQL readiness test are explicit xUnit v3
tests. Therefore, the normal command runs API tests and metadata-based
persistence tests without requiring Docker:

```bash
dotnet test tests/Spendly.IntegrationTests/Spendly.IntegrationTests.csproj
```

To include Testcontainers tests, run from the `backend` directory with a
Docker-compatible container engine available:

```bash
dotnet test tests/Spendly.IntegrationTests/Spendly.IntegrationTests.csproj \
  --settings tests/docker.runsettings
```

When the PostgreSQL fixture cannot start its container, it reports that a
running and accessible Docker-compatible engine is required and preserves the
original exception as the inner exception.

## Adding a database integration test

Create the test class under `Persistence`, attach it to the database collection,
and inherit from the shared base class:

```csharp
[Collection<PostgreSqlDatabaseCollection>]
public sealed class WalletDatabaseTests(
    PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database)
{
    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task Wallet_ShouldPersist()
    {
        await using var context = Database.CreateDbContext();

        // Arrange, act, and assert against the real PostgreSQL database.
    }
}
```

Use `Database.CreateDbContext()` so every database test uses the same migrated
Testcontainer configuration and shared cleanup policy.

## Current limitations

The project does not yet test:

- repositories or Application persistence ports;
- application persistence handlers;
- API feature endpoints backed by PostgreSQL;
- transaction isolation behavior;
- optimistic concurrency;
- database resiliency.
