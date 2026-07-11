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

The project currently contains project references only. It has no `DbContext`,
repositories, database connection, or EF Core packages.

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

Starts `Spendly.Api` in memory through `WebApplicationFactory<Program>`.

Current tests cover:

- API startup;
- root endpoint;
- health checks;
- ProblemDetails;
- configuration behavior;
- OpenAPI and Scalar availability.

Database-backed integration tests will be introduced only after persistence is
implemented.

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
- code style enforcement during build.

`Directory.Packages.props` enables Central Package Management and stores package
versions in one place.

Project files should normally declare package names without repeating versions.

## Commands

Run all commands from this `backend` directory.

Restore:

```bash
dotnet restore Spendly.sln
```

Build:

```bash
dotnet build Spendly.sln
```

Test:

```bash
dotnet test Spendly.sln
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

The current backend does not connect to a database.

The `Infrastructure:Database:Provider` configuration value is a placeholder
that documents the intended configuration shape. It does not create a database
connection.

The PostgreSQL Docker Compose file is optional preparation for a future
milestone and is not required to build, test, or run the current API.
