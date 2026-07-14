# Local Infrastructure

This directory contains local infrastructure definitions for Spendly
development.

## Files

```text
deploy/
├── .env.example
├── docker-compose.yml
└── README.md
```

## PostgreSQL service

The Compose service is named `spendly-postgres` and uses the pinned image:

```text
postgres:17.10
```

Default local values:

```text
Database: spendly
User: spendly
Password: spendly_password
Host port: 5432
Container port: 5432
```

The container name is not fixed, so multiple Compose projects can coexist
without a global Docker name collision.

The service includes a `pg_isready` health check.

## Optional environment file

Copy the example file when local overrides are needed:

```bash
cp deploy/.env.example deploy/.env
```

Available variables:

```text
SPENDLY_POSTGRES_DB
SPENDLY_POSTGRES_USER
SPENDLY_POSTGRES_PASSWORD
SPENDLY_POSTGRES_PORT
```

The defaults and `.env.example` values are for isolated local development only.
They must not be reused in production, staging, shared testing environments, or
publicly reachable database instances.

## Start PostgreSQL

Run from the repository root:

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.yml up -d
```

When `deploy/.env` was not created, omit `--env-file`; Compose will use the
default values declared in `docker-compose.yml`:

```bash
docker compose -f deploy/docker-compose.yml up -d
```

## Check health and state

```bash
docker compose -f deploy/docker-compose.yml ps
```

The service should eventually report a healthy state.

## View logs

```bash
docker compose -f deploy/docker-compose.yml logs -f spendly-postgres
```

Press `Ctrl+C` to stop following logs. The container continues running.

## Stop PostgreSQL

Stop without deleting the service or volume:

```bash
docker compose -f deploy/docker-compose.yml stop
```

Stop and remove the Compose container and network while preserving data:

```bash
docker compose -f deploy/docker-compose.yml down
```

Permanently remove the database volume:

```bash
docker compose -f deploy/docker-compose.yml down --volumes
```

The named volume is `spendly_postgres_data` and is mounted at
`/var/lib/postgresql/data`.

## Current backend integration status

The API requires and validates PostgreSQL connection configuration, but it does
not open a production database connection yet.

The required configuration key is:

```text
ConnectionStrings:SpendlyDatabase
```

The `deploy/.env` file configures the PostgreSQL Compose container only. It is
not automatically loaded by the locally running .NET API.

For local development, store the API connection string through .NET User
Secrets from the `backend` directory:

```bash
dotnet user-secrets set \
  "ConnectionStrings:SpendlyDatabase" \
  "Host=localhost;Port=5432;Database=spendly;Username=spendly;Password=spendly_password" \
  --project src/Spendly.Api/Spendly.Api.csproj
```

The solution still has no production `DbContext`, migrations, repositories, or
database readiness health check.

A test-only EF Core compatibility context uses PostgreSQL Testcontainers to
verify the immutable Domain model. That test is explicit and does not run in a
normal `dotnet test Spendly.sln` invocation.

Run it from the `backend` directory with Docker available:

```bash
dotnet test tests/Spendly.IntegrationTests/Spendly.IntegrationTests.csproj \
  --settings tests/docker.runsettings
```

## Secrets and production environments

Real credentials must not be committed to the repository.

Local development credentials should be supplied through .NET User Secrets.
Deployed environments should use environment variables, CI/CD secret storage,
or a dedicated production secret manager.

The environment variable corresponding to the API connection string is:

```text
ConnectionStrings__SpendlyDatabase
```
