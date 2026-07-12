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
- strongly typed identifiers;
- custom and predefined `Currency` values;
- `Money` as an EF Core complex type;
- the `decimal(19, 4)` money persistence policy;
- income and expense transactions;
- `DateTimeOffset` normalization;
- foreign keys without navigation properties;
- materialization in a new no-tracking `DbContext`.

The compatibility context and configurations are test-only. They are not the
production persistence layer.

## Docker behavior

The PostgreSQL round-trip test is marked as an explicit xUnit v3 test.
Therefore, the normal command below runs API tests and reflection-based shape
tests without requiring Docker:

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
- final production database check constraints;
- repositories;
- application persistence handlers;
- API endpoints backed by PostgreSQL;
- transaction isolation behavior;
- optimistic concurrency;
- database resiliency;
- production database configuration.
