# Spendly Backend

This directory contains the complete Spendly backend solution.

## Current milestone

```text
v0.4 Persistence Layer
```

The current backend includes:

- ASP.NET Core API and Worker hosts;
- validated strongly typed configuration;
- Serilog request and application logging;
- centralized ProblemDetails responses;
- OpenAPI and Scalar documentation;
- liveness and PostgreSQL-backed readiness endpoints;
- the initial Domain model for wallets, categories, transactions, currencies,
  and money;
- production EF Core persistence for PostgreSQL;
- the `InitialCreate` migration;
- unit, API integration, EF Core metadata, PostgreSQL round-trip, migration, and
  readiness tests;
- GitHub Actions CI for restore, formatting, build, migration-model validation,
  dependency reporting, and tests.

Application use cases and feature endpoints are the next architectural layer.
They are intentionally not invented before their real contracts are known.

## Solution structure

```text
backend/
├── src/
│   ├── Spendly.Api/
│   ├── Spendly.Application/
│   ├── Spendly.Domain/
│   ├── Spendly.Infrastructure/
│   └── Spendly.Worker/
├── tests/
│   ├── Spendly.IntegrationTests/
│   └── Spendly.UnitTests/
├── Directory.Build.props
├── Directory.Packages.props
├── README.md
└── Spendly.sln
```

## Projects

### Spendly.Domain

Contains business concepts and invariants:

- `Entity<TId>` and `ValueObject` foundations;
- strongly typed identifiers backed by UUID version 7 values;
- domain errors and exceptions;
- `Currency` and `Money`;
- `Wallet`, `Category`, and `Transaction`.

The project remains independent from ASP.NET Core, EF Core, Npgsql, SQL,
serialization, and infrastructure concerns. Persistence is configured entirely
from `Spendly.Infrastructure`.

See:

- [Domain project documentation](src/Spendly.Domain/README.md)
- [Complete domain model](../docs/architecture/domain-model.md)

### Spendly.Application

Reserved for application use cases and ports.

Expected responsibilities include:

- commands, queries, and handlers;
- use-case orchestration;
- request-independent validation;
- authorization decisions;
- domain-specific persistence ports;
- infrastructure-independent DTOs and projections.

The project currently references `Spendly.Domain`, but no production use cases
have been implemented yet.

### Spendly.Infrastructure

Contains production technical implementations:

- `SpendlyDbContext`;
- EF Core entity configurations;
- converters for strongly typed identifiers, currencies, and enums;
- `Money` complex-property mapping;
- PostgreSQL connection options and startup validation;
- shared `NpgsqlDataSource` registration;
- the PostgreSQL readiness health check;
- EF Core migrations.

Future infrastructure implementations may include domain-specific repository
ports, external clients, messaging, caching, file storage, and clocks.

### Spendly.Api

Hosts the HTTP application and currently provides:

- startup and dependency registration;
- Serilog configuration and request logging;
- centralized exception handling;
- ProblemDetails responses;
- root status endpoint;
- liveness and readiness endpoints;
- OpenAPI generation;
- Scalar UI.

The API contains no wallet, category, transaction, authentication, budget, or
reporting endpoints yet. Future endpoints must call Application use cases
instead of implementing business rules directly.

### Spendly.Worker

Hosts background processing. It currently starts and waits for shutdown without
executing scheduled financial jobs.

Future jobs must call Application use cases. Business rules remain in Domain.

### Spendly.UnitTests

Contains deterministic tests for Domain and future Application behavior. Unit
tests do not start the API, PostgreSQL, Docker, or external services.

### Spendly.IntegrationTests

Contains:

- in-memory API host tests;
- EF Core metadata tests that do not require Docker;
- explicit PostgreSQL Testcontainers tests;
- production migration and schema tests;
- persistence round-trip tests;
- PostgreSQL readiness tests.

See [integration-test documentation](tests/Spendly.IntegrationTests/README.md).

## Dependency rules

Allowed project references:

```text
Application     -> Domain
Infrastructure  -> Application, Domain
Api             -> Application, Infrastructure
Worker          -> Application, Infrastructure
```

Forbidden dependency directions include:

```text
Domain          -> Application, Infrastructure, Api, Worker
Application     -> Infrastructure implementation details, Api, Worker
```

Inner layers define business rules and application contracts. Outer layers
provide transport and technical implementations.

## Build configuration

`Directory.Build.props` applies common settings:

- target framework `net10.0`;
- nullable reference types;
- implicit global usings;
- latest configured analysis level;
- code-style enforcement during build;
- warnings as errors when `ContinuousIntegrationBuild=true`.

`Directory.Packages.props` enables Central Package Management. Project files
normally declare package names without repeating versions.

The repository-local EF Core CLI is pinned in:

```text
../.config/dotnet-tools.json
```

## Restore and build

Run commands from this `backend` directory unless a section says otherwise.

Restore repository-local tools:

```bash
dotnet tool restore
```

Verify the EF Core CLI:

```bash
dotnet tool list --local
dotnet ef --version
```

Restore and build the solution:

```bash
dotnet restore Spendly.sln
dotnet build Spendly.sln
```

Run the default test suite without Docker:

```bash
dotnet test Spendly.sln
```

## Local PostgreSQL

Start PostgreSQL from the repository root:

```bash
cd ..
docker compose -f deploy/docker-compose.yml up -d
cd backend
```

The default local container configuration is:

```text
Host: localhost
Port: 5432
Database: spendly
Username: spendly
Password: spendly_password
```

These credentials are local development defaults only.

See [local infrastructure documentation](../deploy/README.md) for environment
overrides, logs, health, shutdown, and volume removal.

## Configure the API connection string

The required configuration key is:

```text
ConnectionStrings:SpendlyDatabase
```

It is mapped to `PostgreSqlOptions` and validated at startup. Startup fails when
the connection string is missing, malformed, or does not define `Host`,
`Database`, and `Username`.

### .NET User Secrets

Store the local value without changing tracked configuration files:

```bash
dotnet user-secrets set "ConnectionStrings:SpendlyDatabase" "Host=localhost;Port=5432;Database=spendly;Username=spendly;Password=spendly_password" --project src/Spendly.Api/Spendly.Api.csproj
```

Inspect or remove the local value:

```bash
dotnet user-secrets list --project src/Spendly.Api/Spendly.Api.csproj
dotnet user-secrets remove "ConnectionStrings:SpendlyDatabase" --project src/Spendly.Api/Spendly.Api.csproj
```

### Environment variable

.NET maps a double underscore to a configuration colon:

```text
ConnectionStrings__SpendlyDatabase
```

PowerShell:

```powershell
$env:ConnectionStrings__SpendlyDatabase = "Host=localhost;Port=5432;Database=spendly;Username=spendly;Password=spendly_password"
```

Bash:

```bash
export ConnectionStrings__SpendlyDatabase="Host=localhost;Port=5432;Database=spendly;Username=spendly;Password=spendly_password"
```

`deploy/.env` configures the PostgreSQL Compose container. It is not loaded by a
locally started .NET process and does not replace the API connection string.

## Run the API

```bash
dotnet run --project src/Spendly.Api/Spendly.Api.csproj --launch-profile https
```

Default development URLs:

```text
https://localhost:7037
http://localhost:5294
```

Current API surface:

```text
GET /
GET /health/live
GET /health/ready
GET /openapi/{documentName}.json
GET /docs
```

The configured OpenAPI document name is currently `v0.2`. The repository
milestone and the HTTP document name are separate versioning concepts, and the
persistence milestone does not introduce a new feature API contract.

Development documentation endpoints:

```text
https://localhost:7037/openapi/v0.2.json
https://localhost:7037/docs
```

OpenAPI and Scalar are disabled outside Development by the current
configuration.

## Health checks

`GET /health/live` verifies that the API process can serve HTTP requests. It
does not execute dependency checks.

`GET /health/ready` executes all checks tagged `ready`:

- the application self-check;
- the PostgreSQL check registered by Infrastructure.

The PostgreSQL check opens a connection through the shared `NpgsqlDataSource`,
executes `SELECT 1`, and uses a five-second timeout.

Readiness returns:

- `200 OK` when the application and PostgreSQL are healthy;
- `503 Service Unavailable` when PostgreSQL is unavailable.

The check does not create tables, call `EnsureCreated`, apply migrations, or
modify the schema. Responses do not expose connection strings, passwords, or
provider exception details.

Example commands:

```bash
curl --insecure https://localhost:7037/health/live
curl --insecure https://localhost:7037/health/ready
```

## Production persistence layout

The production context is:

```text
src/Spendly.Infrastructure/Persistence/SpendlyDbContext.cs
```

Entity configurations are discovered from the Infrastructure assembly through
`ApplyConfigurationsFromAssembly` and live in:

```text
src/Spendly.Infrastructure/Persistence/Configuration/
```

Converters live in:

```text
src/Spendly.Infrastructure/Persistence/Converters/
```

Migrations live in:

```text
src/Spendly.Infrastructure/Persistence/Migrations/
```

The current context exposes:

```text
DbSet<Wallet> Wallets
DbSet<Category> Categories
DbSet<Transaction> Transactions
```

Complete storage rules are documented in
[Persistence Architecture](../docs/architecture/persistence.md).

## Database representation

The initial migration creates:

```text
wallets
categories
transactions
```

Important mappings:

- strongly typed IDs -> `uuid` with `ValueGeneratedNever()`;
- `Currency` -> `character varying(3)`;
- `Money.Amount` -> `numeric(19,4)`;
- `Money.Currency` -> `currency_code`;
- persisted enums -> `smallint` with check constraints;
- `DateTimeOffset` instants -> `timestamp with time zone`;
- transaction foreign keys -> `ON DELETE RESTRICT`;
- physical identifiers -> explicit lowercase `snake_case`.

## Migration workflow

The API and Worker never apply migrations automatically. Schema changes are an
explicit development or deployment action.

### Create a migration

Choose a descriptive PascalCase name and run:

```bash
dotnet ef migrations add MigrationName --project src/Spendly.Infrastructure/Spendly.Infrastructure.csproj --startup-project src/Spendly.Api/Spendly.Api.csproj --context SpendlyDbContext --output-dir Persistence/Migrations
```

Review all generated files before committing:

- migration `Up` and `Down` operations;
- generated PostgreSQL column types;
- constraints, foreign keys, and indexes;
- data-loss warnings;
- `SpendlyDbContextModelSnapshot` changes.

Check that the model and snapshot agree:

```bash
dotnet ef migrations has-pending-model-changes --project src/Spendly.Infrastructure/Spendly.Infrastructure.csproj --startup-project src/Spendly.Api/Spendly.Api.csproj --context SpendlyDbContext
```

### Apply migrations

The design-time factory configures the Npgsql provider without embedding a
connection string. Supply the target connection explicitly.

```bash
dotnet ef database update --project src/Spendly.Infrastructure/Spendly.Infrastructure.csproj --startup-project src/Spendly.Api/Spendly.Api.csproj --context SpendlyDbContext --connection "Host=localhost;Port=5432;Database=spendly;Username=spendly;Password=spendly_password"
```

Using the environment variable in PowerShell:

```powershell
dotnet ef database update --project src/Spendly.Infrastructure/Spendly.Infrastructure.csproj --startup-project src/Spendly.Api/Spendly.Api.csproj --context SpendlyDbContext --connection $env:ConnectionStrings__SpendlyDatabase
```

Using the environment variable in Bash:

```bash
dotnet ef database update --project src/Spendly.Infrastructure/Spendly.Infrastructure.csproj --startup-project src/Spendly.Api/Spendly.Api.csproj --context SpendlyDbContext --connection "$ConnectionStrings__SpendlyDatabase"
```

List known migrations:

```bash
dotnet ef migrations list --project src/Spendly.Infrastructure/Spendly.Infrastructure.csproj --startup-project src/Spendly.Api/Spendly.Api.csproj --context SpendlyDbContext --connection "Host=localhost;Port=5432;Database=spendly;Username=spendly;Password=spendly_password"
```

### Roll back the database

Update the database to the previous migration identifier:

```bash
dotnet ef database update PreviousMigrationName --project src/Spendly.Infrastructure/Spendly.Infrastructure.csproj --startup-project src/Spendly.Api/Spendly.Api.csproj --context SpendlyDbContext --connection "Host=localhost;Port=5432;Database=spendly;Username=spendly;Password=spendly_password"
```

To roll back every migration in a disposable local database, target `0`:

```bash
dotnet ef database update 0 --project src/Spendly.Infrastructure/Spendly.Infrastructure.csproj --startup-project src/Spendly.Api/Spendly.Api.csproj --context SpendlyDbContext --connection "Host=localhost;Port=5432;Database=spendly;Username=spendly;Password=spendly_password"
```

Rollback commands execute migration `Down` operations and may destroy data.
Production rollback requires a reviewed data and rollout plan.

### Remove an unshared migration

After rolling the database back, remove only the latest migration that has not
been shared or deployed:

```bash
dotnet ef migrations remove --project src/Spendly.Infrastructure/Spendly.Infrastructure.csproj --startup-project src/Spendly.Api/Spendly.Api.csproj --context SpendlyDbContext
```

Do not rewrite an already shared migration. Add a new corrective migration so
every environment retains the same ordered history.

### Generate a deployment script

```bash
dotnet ef migrations script --idempotent --project src/Spendly.Infrastructure/Spendly.Infrastructure.csproj --startup-project src/Spendly.Api/Spendly.Api.csproj --context SpendlyDbContext --output spendly-migrations.sql
```

Review generated SQL before applying it outside an isolated local environment.
The runtime application identity should not require schema-alteration
permissions.

## Why startup migrations are disabled

`Program.cs` registers Infrastructure and starts the application. It does not
call `Database.Migrate()` or `Database.MigrateAsync()`.

This keeps schema deployment explicit and prevents:

- several API replicas racing to update the same schema;
- application startup from becoming an uncontrolled deployment operation;
- runtime identities from requiring DDL permissions;
- an unreviewed destructive migration from being applied automatically;
- health checks or restarts from changing database state.

Database integration tests call `MigrateAsync()` because applying migrations is
the behavior under test. That does not change the production startup policy.

## Why repositories are not implemented yet

Persistence exists, but Application use cases do not.

A repository contract should describe the smallest operation needed by a real
use case, for example saving a wallet or loading transactions for a period. It
should not be created merely because a table exists.

The project also rejects a generic CRUD repository such as
`IGenericRepository<TEntity>` because it would duplicate `DbSet` behavior,
hide use-case intent, and encourage uniform CRUD access where domain entities
have different rules and query needs.

When a use case is implemented:

1. Application defines a domain-specific port;
2. Infrastructure implements it with EF Core;
3. tests protect both the use-case contract and PostgreSQL behavior.

## Tests

Run unit, API, and metadata-based persistence tests without Docker:

```bash
dotnet test Spendly.sln
```

Run every integration test, including explicit Testcontainers tests:

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

Database tests use the production context, production mappings, production
migrations, Npgsql, and PostgreSQL `17.10`. They do not use EF Core InMemory,
SQLite, or `EnsureCreated()` as substitutes for migration verification.

## Continuous integration

Backend CI is defined in:

```text
../.github/workflows/backend-ci.yml
```

The workflow:

1. restores the pinned SDK and local tools;
2. restores NuGet packages;
3. verifies formatting;
4. builds Release with warnings as errors;
5. checks for pending EF Core model changes;
6. reports vulnerable and outdated dependencies;
7. runs unit tests;
8. runs API and metadata integration tests;
9. verifies Docker availability;
10. runs PostgreSQL tests;
11. runs the migration smoke test separately;
12. uploads test results and coverage.

## Secrets

Never commit:

- real database passwords or complete production connection strings;
- `deploy/.env` or other environment-specific secret files;
- access tokens, API keys, signing secrets, private keys, or certificates;
- cloud, CI/CD, or secret-manager credentials;
- database dumps containing real user data.

Tracked files may contain configuration keys, non-secret examples, and isolated
local or ephemeral test defaults. Deployed values belong in protected CI/CD
secrets, environment variables, or a dedicated secret manager.
