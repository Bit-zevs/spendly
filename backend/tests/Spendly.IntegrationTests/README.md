# Spendly.IntegrationTests

Integration tests for Spendly API and Infrastructure boundaries.

## Test groups

The project contains three execution groups:

1. API integration tests;
2. EF Core metadata tests without Docker;
3. explicit PostgreSQL Testcontainers tests.

A normal test run executes the first two groups. Docker-backed tests are enabled
through `tests/docker.runsettings`.

## API tests

API tests start the application in memory through
`WebApplicationFactory<Program>` and send requests through `HttpClient`.

Current scope:

- API host smoke tests;
- root endpoint behavior;
- liveness and readiness endpoint contracts;
- ProblemDetails responses;
- OpenAPI and Scalar availability;
- configuration validation;
- route-collision validation;
- feature-toggle behavior.

These tests use controlled test configuration and do not require Docker.

## Persistence tests without Docker

Metadata-based tests finalize the production Npgsql EF Core model without
opening a database connection.

They protect:

- `SpendlyDbContext` registration and `DbSet` shape;
- strongly typed identifier converters;
- `Currency` conversion;
- `Money` complex-property mapping;
- PostgreSQL column types;
- enum mappings;
- explicit constraint, foreign-key, and index names;
- restrictive delete behavior;
- consistency between the current model and migration metadata.

These tests detect mapping drift quickly, but they do not replace real
PostgreSQL migration and round-trip tests.

## PostgreSQL database tests

Database tests use:

- production `SpendlyDbContext`;
- production EF Core configurations;
- production migrations;
- Npgsql;
- PostgreSQL `17.10`;
- Testcontainers for .NET;
- an xUnit v3 collection fixture.

### Shared database fixture

`PostgreSqlDatabaseFixture`:

1. starts one temporary PostgreSQL container for the shared database
   collection;
2. builds `DbContextOptions<SpendlyDbContext>` from an `NpgsqlDataSource`;
3. applies production migrations through `MigrateAsync()`;
4. creates fresh contexts for tests;
5. reapplies missing migrations and truncates application tables before each
   shared-fixture test while preserving `__EFMigrationsHistory`;
6. disposes the data source and removes the container when the collection
   finishes.

All shared database test classes belong to
`PostgreSqlDatabaseCollection` and inherit from `DatabaseIntegrationTest`.
The collection is serialized so database reset cannot race with another test.

### Initial migration tests

`InitialDatabaseMigrationTests` verifies:

- applied and pending migration state;
- the physical PostgreSQL schema;
- real wallet, category, and transaction write/read round trips;
- materialization through a new no-tracking context;
- restrictive foreign keys;
- rollback to migration `0` and successful reapplication.

### Migration smoke test

`MigrationSmokeTests` uses a dedicated empty PostgreSQL container rather than
the shared migrated fixture.

The smoke test:

1. verifies that the initial `public` schema contains no tables;
2. reads every migration known to the production context;
3. confirms that none is already applied;
4. applies the complete migration pipeline with `MigrateAsync()`;
5. compares known and applied migration identifiers in order;
6. confirms that no pending migration remains;
7. verifies the complete final table set;
8. opens a fresh context and queries every production `DbSet`.

The smoke test does not use `EnsureCreated()` and does not assume a fixed number
of migrations.

### Readiness test

`PostgreSqlReadinessHealthCheckTests` uses a separate empty container. It
verifies that the production readiness check can reach PostgreSQL without
creating tables or changing schema state.

## Commands

Run API and metadata tests without Docker from `backend`:

```bash
dotnet test tests/Spendly.IntegrationTests/Spendly.IntegrationTests.csproj
```

Run every integration test, including explicit Docker tests:

```bash
dotnet test tests/Spendly.IntegrationTests/Spendly.IntegrationTests.csproj --settings tests/docker.runsettings
```

Run explicit PostgreSQL tests except the dedicated migration smoke test:

```bash
dotnet test tests/Spendly.IntegrationTests/Spendly.IntegrationTests.csproj --settings tests/docker.runsettings --filter "Dependency=Docker&FullyQualifiedName!~MigrationSmokeTests"
```

Run only the migration smoke test:

```bash
dotnet test tests/Spendly.IntegrationTests/Spendly.IntegrationTests.csproj --settings tests/docker.runsettings --filter "FullyQualifiedName~MigrationSmokeTests"
```

A running Docker-compatible engine is required for explicit tests. The Compose
service in `deploy/docker-compose.yml` does not need to be running because each
database test owns its temporary container.

If container startup fails, the fixture reports that an accessible
Docker-compatible engine is required and preserves the original exception as
the inner exception.

## Adding a shared-fixture database test

Create the test under `Persistence`, attach it to the database collection, and
inherit from the shared base class:

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

Use `Database.CreateDbContext()` so the test receives the same production
provider setup, migrated database, and cleanup policy.

Use a dedicated container instead of the shared fixture when the test requires
an empty database, an intentionally broken migration state, or an independent
lifecycle.

## Current limitations

The project does not yet test:

- Application persistence ports or repository implementations;
- feature handlers backed by PostgreSQL;
- domain feature HTTP endpoints backed by PostgreSQL;
- transaction isolation scenarios;
- optimistic concurrency;
- retry or resiliency policies.

These tests should be introduced with the corresponding production behavior.

## Related documentation

- [Persistence architecture](../../../docs/architecture/persistence.md)
- [ADR 0003](../../../docs/adr/0003-define-domain-model-persistence-strategy.md)
