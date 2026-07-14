# Spendly

Spendly is a personal finance assistant for tracking expenses, managing budgets,
monitoring subscriptions, planning financial goals, and calculating daily safe
spending.

The main product goal is to help a user answer:

> How much money can I safely spend today without breaking my monthly budget?

## Project status

The current repository milestone is:

```text
v0.3 Domain Model
```

The backend foundation introduced in v0.2 remains in place, including:

- ASP.NET Core API hosting;
- strongly typed configuration;
- Serilog logging;
- centralized ProblemDetails error responses;
- OpenAPI document generation;
- Scalar API documentation;
- liveness and readiness health checks;
- unit and integration test projects;
- backend CI through GitHub Actions.

Version v0.3 adds the first implemented domain model:

- reusable entity and value object abstractions;
- strongly typed identifiers;
- domain errors and domain exceptions;
- `Currency`;
- `Money`;
- `Wallet`;
- `Category`;
- `Transaction`;
- domain unit tests.

The current milestone models business rules only. Wallet, category, and
transaction use cases are not exposed through HTTP endpoints yet.

## Current implementation boundaries

The following functionality is intentionally not implemented yet:

- user registration and authentication;
- wallet, category, and transaction API endpoints;
- application commands and queries;
- repository interfaces and implementations;
- Entity Framework Core;
- `DbContext`;
- database migrations;
- PostgreSQL connectivity;
- persistent wallet balances;
- budgets;
- reports;
- subscriptions;
- financial goals;
- recurring transactions.

A PostgreSQL Docker Compose configuration exists as preparation for a future
persistence milestone, but the current API does not connect to it.

## DateRange decision

`DateRange` is not included in v0.3.

None of the current domain entities requires a date interval, and the correct
semantics of a reusable range are not known yet. Important unresolved questions
include:

- whether the range should use `DateOnly` or `DateTimeOffset`;
- whether both boundaries should be inclusive;
- whether an open-ended range should be allowed;
- whether budgets, reports, goals, and subscriptions require the same type;
- which operations such as `Contains` and `Overlaps` are actually needed.

The value object will be reconsidered when the first real use case requiring a
date range is implemented.

The complete decision is recorded in
[ADR 0002](docs/adr/0002-defer-date-range-until-required.md).

## Planned product features

- User accounts
- Wallets
- Income and expense tracking
- Categories
- Monthly budgets
- Daily safe spend calculation
- Subscriptions
- Financial goals
- Analytics
- Telegram bot
- Web application
- Mobile application later

## Repository structure

```text
spendly/
├── .config/
│   └── dotnet-tools.json
├── backend/
│   ├── src/
│   │   ├── Spendly.Api/
│   │   ├── Spendly.Application/
│   │   ├── Spendly.Domain/
│   │   ├── Spendly.Infrastructure/
│   │   └── Spendly.Worker/
│   ├── tests/
│   │   ├── Spendly.UnitTests/
│   │   └── Spendly.IntegrationTests/
│   └── Spendly.sln
├── deploy/
│   └── docker-compose.yml
├── docs/
│   ├── adr/
│   ├── architecture/
│   └── product/
├── global.json
└── README.md
```

## Backend architecture

Spendly currently uses a modular monolith with Clean Architecture Lite.

The dependency direction is:

```text
Spendly.Domain
      ↑
Spendly.Application
      ↑
Spendly.Infrastructure
      ↑
Spendly.Api / Spendly.Worker
```

The actual project references are:

```text
Spendly.Domain
  └── no project dependencies

Spendly.Application
  └── Spendly.Domain

Spendly.Infrastructure
  ├── Spendly.Application
  └── Spendly.Domain

Spendly.Api
  ├── Spendly.Application
  └── Spendly.Infrastructure

Spendly.Worker
  ├── Spendly.Application
  └── Spendly.Infrastructure
```

The Domain project is the innermost layer. Business rules must not depend on
ASP.NET Core, HTTP, Entity Framework Core, PostgreSQL, background workers, or
other delivery and persistence mechanisms.

More information:

- [Documentation index](docs/README.md)
- [Architecture overview](docs/architecture/overview.md)
- [Domain model](docs/architecture/domain-model.md)
- [Backend guide](backend/README.md)

## Technology stack

Currently used:

- C#
- .NET 10
- ASP.NET Core
- Serilog
- OpenAPI
- Scalar
- xUnit v3
- GitHub Actions
- repository-local .NET tools
- centralized NuGet package version management

Prepared for future milestones:

- PostgreSQL
- Entity Framework Core
- Docker-based local infrastructure

## Requirements

Required:

- .NET 10 SDK

Optional:

- Docker Desktop;
- JetBrains Rider;
- Visual Studio;
- another C# IDE with .NET 10 support.

The SDK version is pinned in:

```text
global.json
```

Shared build settings are stored in:

```text
backend/Directory.Build.props
```

Centralized NuGet package versions are stored in:

```text
backend/Directory.Packages.props
```

Repository-local .NET tools are pinned in:

```text
.config/dotnet-tools.json
```

## Restore local .NET tools

From the repository root:

```bash
dotnet tool restore
```

This restores the exact `dotnet-ef` version declared by the repository. A
global installation of `dotnet-ef` is not required.

Verify the restored tool:

```bash
dotnet tool list --local
dotnet ef --version
```

## Restore backend dependencies

From the repository root:

```bash
cd backend
dotnet restore Spendly.sln
```

## Build the backend

From the `backend` directory:

```bash
dotnet build Spendly.sln
```

The command builds:

- `Spendly.Api`;
- `Spendly.Application`;
- `Spendly.Domain`;
- `Spendly.Infrastructure`;
- `Spendly.Worker`;
- `Spendly.UnitTests`;
- `Spendly.IntegrationTests`.

## Run tests

From the `backend` directory:

```bash
dotnet test Spendly.sln
```

The test projects are:

```text
tests/Spendly.UnitTests
tests/Spendly.IntegrationTests
```

Unit tests verify domain behavior in isolation.

Integration tests include two groups:

- API and model-shape tests that do not require external services;
- an explicit EF Core compatibility test that uses PostgreSQL Testcontainers.

A normal `dotnet test Spendly.sln` run does not execute explicit tests and does
not require Docker. To include the PostgreSQL round-trip, run:

```bash
dotnet test tests/Spendly.IntegrationTests/Spendly.IntegrationTests.csproj \
  --settings tests/docker.runsettings
```

## Run the API locally

From the `backend` directory:

```bash
dotnet run --project src/Spendly.Api/Spendly.Api.csproj --launch-profile https
```

Local URLs:

```text
https://localhost:7037
http://localhost:5294
```

Root status endpoint:

```text
GET https://localhost:7037/
```

Example response:

```json
{
  "application": "Spendly API",
  "status": "Running"
}
```

## Current API surface

The API currently exposes backend foundation endpoints only:

```text
GET /
GET /health/live
GET /health/ready
GET /openapi/{documentName}.json
GET /docs
```

There are no wallet, category, transaction, budget, authentication, or
reporting endpoints yet.

The repository milestone version and the API document version are separate
concepts. The API configuration currently keeps the document name `v0.2`
because the v0.3 work adds a domain model without changing the HTTP contract.

In the Development environment:

```text
https://localhost:7037/docs
https://localhost:7037/openapi/v0.2.json
```

OpenAPI and Scalar are disabled outside Development unless the configuration
and environment rules are changed intentionally.

## Health checks

Liveness:

```text
GET https://localhost:7037/health/live
```

Readiness:

```text
GET https://localhost:7037/health/ready
```

The current health checks verify the application process only.

They do not check PostgreSQL or other external dependencies because those
dependencies are not connected yet.

## Error handling

The API uses ASP.NET Core ProblemDetails as its standard HTTP error format.

A typical error response contains:

```json
{
  "type": "about:blank",
  "title": "Not Found",
  "status": 404,
  "detail": "The requested resource was not found.",
  "instance": "/unknown",
  "code": "not_found",
  "traceId": "..."
}
```

Unhandled exceptions are logged through Serilog and converted to safe
`500 Internal Server Error` responses without exposing stack traces or internal
implementation details to clients.

Domain errors are currently used inside the domain model. Mapping domain errors
to HTTP responses will be introduced together with application use cases and
domain API endpoints.

## PostgreSQL configuration and database status

The API requires a PostgreSQL connection string named:

```text
ConnectionStrings:SpendlyDatabase
```

The connection string is mapped to strongly typed `PostgreSqlOptions` and
validated during application startup. Startup fails when the value is missing,
malformed, or does not define `Host`, `Database`, and `Username`.

For local development, start PostgreSQL from the repository root:

```bash
docker compose -f deploy/docker-compose.yml up -d
```

Then store the local connection string through .NET User Secrets from the
`backend` directory:

```bash
dotnet user-secrets set \
  "ConnectionStrings:SpendlyDatabase" \
  "Host=localhost;Port=5432;Database=spendly;Username=spendly;Password=spendly_password" \
  --project src/Spendly.Api/Spendly.Api.csproj
```

On PowerShell:

```powershell
dotnet user-secrets set `
  "ConnectionStrings:SpendlyDatabase" `
  "Host=localhost;Port=5432;Database=spendly;Username=spendly;Password=spendly_password" `
  --project src/Spendly.Api/Spendly.Api.csproj
```

The same configuration can be supplied through an environment variable:

```text
ConnectionStrings__SpendlyDatabase
```

For example, in PowerShell:

```powershell
$env:ConnectionStrings__SpendlyDatabase = "Host=localhost;Port=5432;Database=spendly;Username=spendly;Password=spendly_password"
```

The credentials above are isolated local-development defaults from the Docker
Compose configuration. They must not be reused in production, staging, shared
testing environments, or publicly reachable database instances.

Production Entity Framework Core persistence is still not configured. The
project currently has:

- no production `DbContext`;
- no production entity configurations;
- no repositories;
- no migrations;
- no database readiness health check.

The API validates and stores the connection configuration, but does not open a
database connection yet.

A test-only EF Core compatibility context and PostgreSQL Testcontainers test
verify that the immutable Domain model can be materialized by the real Npgsql
provider. The storage contract is accepted in
[ADR 0003](../docs/adr/0003-define-domain-model-persistence-strategy.md), while
the production context and migrations remain intentionally deferred.

## Continuous integration

Backend CI is configured in:

```text
.github/workflows/backend-ci.yml
```

The workflow:

1. checks out the repository;
2. installs the SDK from `global.json`;
3. restores repository-local .NET tools;
4. verifies the local EF Core CLI;
5. restores the solution;
6. verifies formatting;
7. builds in Release with warnings treated as errors;
8. audits vulnerable and outdated dependencies;
9. runs unit tests with coverage;
10. runs integration and explicit Docker compatibility tests with coverage;
11. uploads test results and coverage artifacts.

The workflow is triggered for backend-related pull requests and pushes to
`main`.
