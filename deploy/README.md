# Local Infrastructure and Database Deployment

This directory contains the Docker Compose definition used for local Spendly
PostgreSQL development.

## Files

```text
deploy/
├── .env.example
├── docker-compose.yml
└── README.md
```

## PostgreSQL service

The Compose service is named:

```text
spendly-postgres
```

It uses the pinned image:

```text
postgres:17.10
```

Default isolated local values:

```text
Database: spendly
Username: spendly
Password: spendly_password
Host port: 5432
Container port: 5432
```

The Compose configuration provides:

- a PostgreSQL container;
- host-port override support;
- a named volume for persistent local data;
- a `pg_isready` health check;
- environment overrides through `deploy/.env`.

The container name is not fixed, so separate Compose projects can coexist
without a global container-name collision.

## Optional environment overrides

Create an untracked local environment file from the repository root:

```bash
cp deploy/.env.example deploy/.env
```

PowerShell:

```powershell
Copy-Item deploy/.env.example deploy/.env
```

Available variables:

```text
SPENDLY_POSTGRES_DB
SPENDLY_POSTGRES_USER
SPENDLY_POSTGRES_PASSWORD
SPENDLY_POSTGRES_PORT
```

`deploy/.env` is ignored by Git. It configures the Compose container only and is
not automatically loaded by a locally running .NET process.

## Start PostgreSQL

Without a local `.env` file:

```bash
docker compose -f deploy/docker-compose.yml up -d
```

With `deploy/.env`:

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.yml up -d
```

## Check state and health

```bash
docker compose -f deploy/docker-compose.yml ps
```

The service should eventually report `healthy`.

The health check runs `pg_isready` inside the container. It verifies that
PostgreSQL accepts connections; it does not verify that Spendly migrations have
been applied.

## View logs

```bash
docker compose -f deploy/docker-compose.yml logs -f spendly-postgres
```

Press `Ctrl+C` to stop following logs. The container continues running.

## Stop or remove local infrastructure

Stop without removing the container or volume:

```bash
docker compose -f deploy/docker-compose.yml stop
```

Remove the Compose container and network while preserving database data:

```bash
docker compose -f deploy/docker-compose.yml down
```

Remove the container, network, and database volume:

```bash
docker compose -f deploy/docker-compose.yml down --volumes
```

The named volume is:

```text
spendly_postgres_data
```

It is mounted at:

```text
/var/lib/postgresql/data
```

Removing the volume permanently deletes local database data.

## Connect the API

The API requires:

```text
ConnectionStrings:SpendlyDatabase
```

From `backend`, store the default local connection string with .NET User
Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:SpendlyDatabase" "Host=localhost;Port=5432;Database=spendly;Username=spendly;Password=spendly_password" --project src/Spendly.Api/Spendly.Api.csproj
```

The equivalent environment variable is:

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

The API validates the connection string during startup and registers a shared
`NpgsqlDataSource`, `SpendlyDbContext`, and PostgreSQL readiness check.

## Apply migrations

Starting PostgreSQL does not create the Spendly schema. Apply migrations as an
explicit step from `backend`:

```bash
dotnet tool restore
dotnet ef database update --project src/Spendly.Infrastructure/Spendly.Infrastructure.csproj --startup-project src/Spendly.Api/Spendly.Api.csproj --context SpendlyDbContext --connection "Host=localhost;Port=5432;Database=spendly;Username=spendly;Password=spendly_password"
```

The API and Worker do not call `Database.Migrate()` or
`Database.MigrateAsync()` at startup.

This separation keeps application startup from changing schema state and avoids
migration races when several application replicas start together.

For a deployed environment, apply a reviewed SQL script, an EF Core migration
bundle, or another controlled one-time migration job before the new application
version starts serving traffic.

## Run the API and verify readiness

From `backend`:

```bash
dotnet run --project src/Spendly.Api/Spendly.Api.csproj --launch-profile https
```

Then check:

```bash
curl --insecure https://localhost:7037/health/live
curl --insecure https://localhost:7037/health/ready
```

Expected behavior:

- liveness remains healthy while the API process can respond;
- readiness is healthy only when the self-check and PostgreSQL check succeed;
- readiness returns `503 Service Unavailable` when PostgreSQL is unavailable;
- readiness does not create tables or apply migrations.

## Database integration tests

Database tests use temporary PostgreSQL containers, not the Compose service.
A Docker-compatible engine must be running, but the local `spendly-postgres`
service does not need to be started.

From `backend`:

```bash
dotnet test tests/Spendly.IntegrationTests/Spendly.IntegrationTests.csproj --settings tests/docker.runsettings
```

The tests use production EF Core mappings and migrations, then remove their
containers after the test lifecycle completes.

## Production deployment principles

A production deployment should:

1. supply the connection string through protected environment configuration or
   a secret manager;
2. apply reviewed migrations once for the target database;
3. start the new API and Worker version without automatic schema mutation;
4. wait for readiness before routing traffic;
5. retain a rollout and data-recovery plan for destructive migrations.

The runtime database identity should not require schema-alteration permissions
unless the selected deployment mechanism explicitly uses that identity for the
migration step.

## Secrets

Do not commit:

- real PostgreSQL passwords;
- production or shared-environment connection strings;
- `deploy/.env`;
- cloud, CI/CD, or secret-manager credentials;
- private keys or client certificates;
- database backups containing real data.

The values in `.env.example`, `docker-compose.yml`, and Testcontainers fixtures
are isolated local or ephemeral defaults. They must not be reused for staging,
production, shared test environments, or publicly reachable databases.
