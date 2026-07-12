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
- configuration-driven endpoint tests.

## Persistence compatibility tests

The persistence compatibility tests use:

- Entity Framework Core;
- Npgsql;
- PostgreSQL;
- Testcontainers for .NET.

They verify that the immutable Domain model can be saved and materialized
without public setters or EF Core attributes.

The current compatibility test covers:

- private entity constructors;
- `Entity<TId>`;
- strongly typed identifiers;
- `Currency`;
- `Money` as an EF Core complex type;
- `DateTimeOffset`;
- nullable `Transaction.UpdatedAt`;
- foreign keys without navigation properties;
- materialization in a new no-tracking `DbContext`.

The compatibility context and configurations are test-only. They are not the
production persistence layer.

## Requirements

API-only tests do not require Docker.

Database-backed compatibility tests require a running Docker-compatible
container engine, such as Docker Desktop.

Testcontainers starts and disposes the PostgreSQL container automatically.

## Running tests

From the `backend` directory:

```bash
dotnet test tests/Spendly.IntegrationTests/Spendly.IntegrationTests.csproj
```

To run only the EF Core compatibility test:

```bash
dotnet test tests/Spendly.IntegrationTests/Spendly.IntegrationTests.csproj \
--filter FullyQualifiedName~EfCoreDomainModelCompatibilityTests
```

### Current limitations

The project does not yet test:
* production migrations;
* repositories;
* application persistence handlers;
* API endpoints backed by PostgreSQL;
* transaction isolation behavior;
* optimistic concurrency;
* database resiliency;
* production database configuration.
