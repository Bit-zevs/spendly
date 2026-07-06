# Spendly

Spendly is a personal finance assistant for expense tracking, budgeting, subscriptions, financial goals and daily safe spending calculation.

The main goal of Spendly is to help users understand how much money they can safely spend today without breaking their monthly budget.

## Project status

Spendly is in the initial backend foundation stage.

Current backend version:

```text
v0.2 Backend Foundation
```

At this stage the backend contains the basic API foundation:

- ASP.NET Core API project structure
- application configuration through strongly typed options
- Serilog request and application logging
- global error handling with ProblemDetails
- OpenAPI document generation
- Scalar API documentation UI
- liveness and readiness health check endpoints
- unit and integration test projects
- backend CI through GitHub Actions

Database access and Entity Framework Core are not part of v0.2 yet.

## Planned features

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
└── README.md
```

## Backend technology stack

Currently used in v0.2:

- C#
- .NET 10
- ASP.NET Core
- Serilog
- OpenAPI
- Scalar
- xUnit
- GitHub Actions

Prepared for future stages, but not used by the API in v0.2:

- PostgreSQL
- Entity Framework Core

## Requirements

Required for backend development:

- .NET 10 SDK

Optional:

- Docker Desktop
- JetBrains Rider, Visual Studio or another C# IDE

The SDK version is pinned in `global.json`.

## Backend setup

All backend commands should be executed from the repository root unless stated otherwise.

Go to the backend directory:

```bash
cd backend
```

Restore dependencies:

```bash
dotnet restore Spendly.sln
```

## Build backend

From the `backend` directory:

```bash
dotnet build Spendly.sln
```

This builds the full backend solution, including:

- `Spendly.Api`
- `Spendly.Application`
- `Spendly.Domain`
- `Spendly.Infrastructure`
- `Spendly.Worker`
- `Spendly.UnitTests`
- `Spendly.IntegrationTests`

## Run tests

From the `backend` directory:

```bash
dotnet test Spendly.sln
```

The current test projects are:

```text
tests/Spendly.UnitTests
tests/Spendly.IntegrationTests
```

Integration tests use the ASP.NET Core test host through `WebApplicationFactory<Program>`.

They do not require PostgreSQL or any external database in v0.2.

## Run API locally

From the `backend` directory:

```bash
dotnet run --project src/Spendly.Api/Spendly.Api.csproj --launch-profile https
```

The `https` launch profile starts the API with the `Development` environment.

Local API URLs:

```text
https://localhost:7037
http://localhost:5294
```

The main local HTTPS URL is:

```text
https://localhost:7037
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

## Scalar and OpenAPI

The project uses Scalar as the API documentation UI.

Scalar UI is available at:

```text
https://localhost:7037/docs
```

OpenAPI JSON is available at:

```text
https://localhost:7037/openapi/v0.2.json
```

OpenAPI and Scalar are enabled only in the `Development` environment.

In `appsettings.json`, OpenAPI is disabled by default:

```json
{
  "OpenApi": {
    "Enabled": false
  }
}
```

In `appsettings.Development.json`, OpenAPI is enabled for local development:

```json
{
  "OpenApi": {
    "Enabled": true
  }
}
```

The API also checks that the application is running in `Development` before mapping Scalar and OpenAPI endpoints.

## Health checks

The API exposes two health check endpoints.

### Liveness

```text
GET https://localhost:7037/health/live
```

The liveness endpoint checks whether the API process is alive and able to respond to HTTP requests.

In v0.2 this endpoint does not check the database or external services.

### Readiness

```text
GET https://localhost:7037/health/ready
```

The readiness endpoint checks whether the API is ready to serve traffic.

In v0.2 readiness contains the internal `self` health check only.

The current readiness check confirms that the application is running.

Example response:

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0000000",
  "entries": {
    "self": {
      "status": "Healthy",
      "description": "Application is running.",
      "duration": "00:00:00.0000000"
    }
  }
}
```

## Error handling

The API uses a centralized error response format based on ASP.NET Core ProblemDetails.

Errors are returned as:

```text
application/problem+json
```

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

Field meaning:

```text
type      - error type or documentation URL
title     - short error title
status    - HTTP status code
detail    - human-readable error description
instance  - request path where the error happened
code      - Spendly internal error code
traceId   - request trace identifier for logs and debugging
```

Unhandled exceptions are converted to a safe `500 Internal Server Error` response.

The client receives a generic error response without stack traces or internal implementation details.

Detailed exception information is written to application logs through Serilog.

Validation errors use the same ProblemDetails format and include validation details.

## Database and EF Core status

PostgreSQL and Entity Framework Core are not part of the implemented API in v0.2.

The repository contains a Docker Compose file for PostgreSQL:

```text
deploy/docker-compose.yml
```

This file is prepared for future backend stages.

Current v0.2 behavior:

- the API does not connect to PostgreSQL
- there is no EF Core `DbContext`
- there are no EF Core migrations
- health checks do not check database connectivity
- tests do not require a running database
- local API startup does not require Docker

Database integration will be added in a later backend milestone.

## Optional PostgreSQL container

PostgreSQL can be started from the repository root if needed for future work:

```bash
docker compose -f deploy/docker-compose.yml up -d
```

For v0.2 backend development this step is optional and not required.

## Useful backend commands

From the repository root:

```bash
cd backend
dotnet restore Spendly.sln
dotnet build Spendly.sln
dotnet test Spendly.sln
dotnet run --project src/Spendly.Api/Spendly.Api.csproj --launch-profile https
```

## CI

Backend CI is configured through GitHub Actions.

Workflow file:

```text
.github/workflows/backend-ci.yml
```

The CI pipeline restores, builds and tests the backend solution.
