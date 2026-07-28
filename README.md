# Spendly

Spendly is being built as a personal finance assistant for tracking money,
managing budgets, monitoring subscriptions, planning financial goals, and
calculating a safe amount to spend each day.

The product is being developed as a modular monolith with Clean Architecture
Lite. The repository currently contains the backend foundation, the first
Domain model, and a production PostgreSQL persistence layer.

## Current milestone

```text
v0.4 Persistence Layer
```

The current milestone includes:

- .NET 10 backend solution;
- ASP.NET Core API and Worker hosts;
- immutable Domain entities and value objects;
- strongly typed UUID version 7 identifiers;
- Entity Framework Core and Npgsql integration;
- production `SpendlyDbContext` and explicit entity configurations;
- PostgreSQL migration `InitialCreate`;
- PostgreSQL readiness health check;
- local PostgreSQL through Docker Compose;
- metadata, migration, round-trip, and readiness integration tests;
- CI validation for formatting, build, tests, vulnerabilities, and pending
  EF Core model changes.

Application use cases, repositories, authentication, and domain feature
endpoints are not implemented yet. Product vision and MVP documents describe
target capabilities; the delivered state is tracked separately.

## Repository structure

```text
spendly/
├── .config/
│   └── dotnet-tools.json
├── .github/
│   └── workflows/
├── backend/
│   ├── src/
│   │   ├── Spendly.Api/
│   │   ├── Spendly.Application/
│   │   ├── Spendly.Domain/
│   │   ├── Spendly.Infrastructure/
│   │   └── Spendly.Worker/
│   ├── tests/
│   │   ├── Spendly.IntegrationTests/
│   │   └── Spendly.UnitTests/
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

## Requirements

Required:

- .NET 10 SDK compatible with `global.json`.

Required for PostgreSQL database tests and local infrastructure:

- Docker Desktop or another Docker-compatible container engine.

The repository pins:

- the .NET SDK in `global.json`;
- repository-local .NET tools in `.config/dotnet-tools.json`;
- NuGet versions in `backend/Directory.Packages.props`;
- the local PostgreSQL image in `deploy/docker-compose.yml`.

## Quick start

Restore the repository-local EF Core tool:

```bash
dotnet tool restore
```

Restore, build, and run the default test suite:

```bash
cd backend
dotnet restore Spendly.sln
dotnet build Spendly.sln
dotnet test Spendly.sln
```

Return to the repository root and start local PostgreSQL:

```bash
cd ..
docker compose -f deploy/docker-compose.yml up -d
cd backend
```

Configure the API connection string from the `backend` directory:

```bash
dotnet user-secrets set "ConnectionStrings:SpendlyDatabase" "Host=localhost;Port=5432;Database=spendly;Username=spendly;Password=spendly_password" --project src/Spendly.Api/Spendly.Api.csproj
```

Apply the current migrations:

```bash
dotnet ef database update --project src/Spendly.Infrastructure/Spendly.Infrastructure.csproj --startup-project src/Spendly.Api/Spendly.Api.csproj --context SpendlyDbContext --connection "Host=localhost;Port=5432;Database=spendly;Username=spendly;Password=spendly_password"
```

Run the API:

```bash
dotnet run --project src/Spendly.Api/Spendly.Api.csproj --launch-profile https
```

Default development endpoints:

```text
GET https://localhost:7037/
GET https://localhost:7037/health/live
GET https://localhost:7037/health/ready
GET https://localhost:7037/openapi/v0.2.json
GET https://localhost:7037/docs
```

The API document name remains `v0.2` because the persistence milestone does not
change the public HTTP feature contract.

## Documentation

- [Backend development guide](backend/README.md)
- [Documentation index](docs/README.md)
- [Product delivery status](docs/product/delivery-status.md)
- [Architecture overview](docs/architecture/overview.md)
- [Domain model](docs/architecture/domain-model.md)
- [Persistence architecture](docs/architecture/persistence.md)
- [Local infrastructure](deploy/README.md)
- [Integration tests](backend/tests/Spendly.IntegrationTests/README.md)

## Security

Do not commit real connection strings, passwords, access tokens, private keys,
certificates, or environment-specific secret files. Use .NET User Secrets for
local API development and a secret manager or protected environment variables
for deployed environments.
