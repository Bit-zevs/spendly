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

Integration tests start the API in memory through
`WebApplicationFactory<Program>` and do not currently require PostgreSQL,
Docker, or any external services.

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
  "type": "https://httpstatuses.com/404",
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

## Database status

Entity Framework Core is not installed or configured in the current backend.

The project currently has:

- no `DbContext`;
- no entity configurations;
- no repositories;
- no migrations;
- no connection string used by the API;
- no database health check;
- no database-backed integration tests.

The Docker Compose file is preparation for a future persistence milestone:

```text
deploy/docker-compose.yml
```

Starting PostgreSQL is currently optional:

```bash
docker compose -f deploy/docker-compose.yml up -d
```

See [deploy/README.md](deploy/README.md) before using it.

## Continuous integration

Backend CI is configured in:

```text
.github/workflows/backend-ci.yml
```

The workflow:

1. checks out the repository;
2. installs the SDK from `global.json`;
3. restores the solution;
4. builds it in Release configuration;
5. runs unit tests;
6. runs integration tests.

The workflow is triggered for backend-related pull requests and pushes to
`main`.
