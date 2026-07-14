# Spendly Backend

This directory contains the complete Spendly backend solution.

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
│   ├── Spendly.UnitTests/
│   └── Spendly.IntegrationTests/
├── Directory.Build.props
├── Directory.Packages.props
└── Spendly.sln
```

## Projects

### Spendly.Domain

Contains the business model and business invariants.

Current implementation:

- entity equality by strongly typed identity;
- value object equality by value components;
- strongly typed identifiers backed by version 7 UUIDs;
- domain errors and domain exceptions;
- `Currency`;
- `Money`;
- `Wallet`;
- `Category`;
- `Transaction`.

This project has no references to other Spendly projects and must remain free
from ASP.NET Core, Entity Framework Core, database, HTTP, and infrastructure
concerns.

See:

- [Domain project documentation](src/Spendly.Domain/README.md)
- [Complete domain model](../docs/architecture/domain-model.md)

### Spendly.Application

Reserved for application use cases and orchestration.

Future responsibilities include:

- commands and queries;
- use-case handlers;
- application services;
- request validation;
- repository contracts;
- unit-of-work abstractions;
- authorization policies;
- DTO mapping that is independent from HTTP.

The project currently references `Spendly.Domain`, but no application use cases
have been implemented yet.

### Spendly.Infrastructure

Reserved for technical implementations required by the Application layer.

Future responsibilities include:

- Entity Framework Core;
- PostgreSQL persistence;
- repository implementations;
- database migrations;
- external service clients;
- messaging implementations;
- system clock and file storage implementations.

The project already references EF Core, EF Core Design, and Npgsql as
preparation for persistence work and compatibility verification. It still has
no production `DbContext`, entity configurations, repositories, database
connection, or migrations.

### Spendly.Api

Hosts the ASP.NET Core HTTP application.

Current responsibilities include:

- application startup;
- dependency registration;
- strongly typed configuration;
- Serilog integration;
- request logging;
- centralized exception handling;
- ProblemDetails responses;
- root status endpoint;
- liveness and readiness endpoints;
- OpenAPI generation;
- Scalar documentation.

Domain feature endpoints are not implemented yet.

The API must not contain domain business rules. Future endpoints should call
Application use cases and translate transport-specific requests and responses.

### Spendly.Worker

Hosts background processing.

The current worker is a minimal executable host that starts and waits until
shutdown. It does not execute scheduled financial jobs yet.

Future background responsibilities may include:

- recurring subscription processing;
- scheduled notifications;
- report generation;
- maintenance jobs;
- message queue consumers.

Business rules must remain in Domain, while orchestration should remain in
Application.

### Spendly.UnitTests

Contains isolated tests for Domain and future Application behavior.

Unit tests must not require:

- ASP.NET Core hosting;
- PostgreSQL;
- Docker;
- network calls;
- external services.

### Spendly.IntegrationTests

Contains two integration-test groups.

API tests start `Spendly.Api` in memory through
`WebApplicationFactory<Program>` and cover:

- API startup;
- root endpoint;
- health checks;
- ProblemDetails;
- configuration behavior;
- OpenAPI and Scalar availability.

Persistence compatibility tests use EF Core, Npgsql, PostgreSQL, and
Testcontainers to verify that the immutable Domain model can be saved and
materialized without public setters or EF Core attributes. The compatibility
`DbContext` and configurations are test-only and are not the production
persistence layer.

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
Domain          -> Application
Domain          -> Infrastructure
Domain          -> Api
Application     -> Api
Application     -> Infrastructure implementation details
```

The inner layers define business and application contracts. Outer layers
provide technical implementations.

## Shared build configuration

`Directory.Build.props` applies common settings to all backend projects:

- target framework `net10.0`;
- nullable reference types;
- implicit global usings;
- latest available analysis level;
- code style enforcement during build;
- warnings as errors for continuous-integration builds.

`Directory.Packages.props` enables Central Package Management and stores package
versions in one place.

Project files should normally declare package names without repeating versions.

Repository-local .NET CLI tools are pinned in
[`../.config/dotnet-tools.json`](../.config/dotnet-tools.json). The manifest
currently provides `dotnet-ef` for future production migrations and other EF
Core design-time operations.

## Commands

Run all commands from this `backend` directory. The .NET SDK finds the local
tool manifest in the repository root because `backend` is inside its scope.

Restore repository-local tools after cloning or after the manifest changes:

```bash
dotnet tool restore
```

Verify the local EF Core CLI:

```bash
dotnet tool list --local
dotnet ef --version
```

Restore NuGet dependencies:

```bash
dotnet restore Spendly.sln
```

Build:

```bash
dotnet build Spendly.sln
```

Run all non-explicit tests without Docker:

```bash
dotnet test Spendly.sln
```

Run integration tests including the explicit PostgreSQL Testcontainers test:

```bash
dotnet test tests/Spendly.IntegrationTests/Spendly.IntegrationTests.csproj \
  --settings tests/docker.runsettings
```

Run API:

```bash
dotnet run --project src/Spendly.Api/Spendly.Api.csproj --launch-profile https
```

Run Worker:

```bash
dotnet run --project src/Spendly.Worker/Spendly.Worker.csproj
```

## Current persistence status

The API requires a PostgreSQL connection string named:

```text
ConnectionStrings:SpendlyDatabase
```

The value is validated when the application starts. The API fails fast when
the connection string is missing, malformed, or does not define `Host`,
`Database`, and `Username`.

The connection string must be provided through .NET User Secrets for local
development or through an environment variable or a secret manager in deployed
environments. Real credentials must not be committed to the repository.

The production API still does not open a database connection because the
production `DbContext`, entity configurations, migrations, and repositories are
intentionally deferred to the persistence implementation milestone.

The PostgreSQL Docker Compose file provides a local database instance. The
accepted storage contract is documented in
[ADR 0003](docs/adr/0003-define-domain-model-persistence-strategy.md).

The PostgreSQL compatibility test is explicit, so normal test execution does
not require Docker. Metadata-based persistence strategy tests run without a
database connection. A Docker-compatible container engine is required only
when the explicit test is enabled through `tests/docker.runsettings`.
