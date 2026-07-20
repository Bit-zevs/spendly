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

## Persistence compatibility tests

The persistence compatibility tests use:

- Entity Framework Core;
- Npgsql;
- PostgreSQL;
- Testcontainers for .NET.

They verify that the immutable Domain model can be saved and materialized
without public setters or EF Core attributes.

The compatibility suite covers:

- private entity constructors;
- read-only public properties and absence of public setters;
- `Entity<TId>`;
- strongly typed identifiers mapped to PostgreSQL `uuid`;
- custom and predefined `Currency` values mapped to
  `character varying(3)`;
- `Money` as an EF Core complex type;
- the `numeric(19,4)` money persistence policy;
- enums mapped to PostgreSQL `smallint`;
- explicit check constraints, key names, foreign-key names, and index names;
- income and expense transactions;
- `DateTimeOffset` normalization and `timestamp with time zone` mapping;
- nullable transaction `UpdatedAt` mapping;
- restrictive foreign keys without Domain navigation properties;
- materialization in a new no-tracking `DbContext`.

The accepted rules are documented in
[ADR 0003](../../../docs/adr/0003-define-domain-model-persistence-strategy.md).

The compatibility context and configurations are test-only. They are not the
production persistence layer.

## Test groups

`EfCoreDomainModelShapeTests` protects the persistence-friendly shape of the
Domain model without opening a database connection.

`EfCorePersistenceStrategyTests` inspects the finalized Npgsql model without
opening a database connection. It protects approved storage types, converters,
constraint names, index names, and delete behavior.

`EfCoreDomainModelCompatibilityTests` performs the real PostgreSQL round-trip
through Testcontainers.

## Docker behavior

The PostgreSQL round-trip test is marked as an explicit xUnit v3 test.
Therefore, the normal command below runs API tests and metadata-based
persistence tests without requiring Docker:

```bash
dotnet test tests/Spendly.IntegrationTests/Spendly.IntegrationTests.csproj
```

To include the PostgreSQL Testcontainers test, run from the `backend`
directory with a Docker-compatible container engine available:

```bash
dotnet test tests/Spendly.IntegrationTests/Spendly.IntegrationTests.csproj \
  --settings tests/docker.runsettings
```

Testcontainers starts and disposes the PostgreSQL container automatically.

## Current limitations

The project does not yet test:

- production migrations;
- the production `SpendlyDbContext`;
- repositories or application persistence ports;
- application persistence handlers;
- API endpoints backed by PostgreSQL;
- transaction isolation behavior;
- optimistic concurrency;
- database resiliency;
- production database configuration.

When production persistence is introduced, migration tests must use the real
Npgsql provider and PostgreSQL Testcontainers. The temporary compatibility
context should then be removed after equivalent production coverage exists.
