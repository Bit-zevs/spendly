# Local Infrastructure

This directory contains local infrastructure definitions prepared for Spendly
development.

## Current contents

```text
deploy/
├── docker-compose.yml
└── README.md
```

The current Docker Compose configuration defines a local PostgreSQL service.

## PostgreSQL service

Compose service name:

```text
spendly-postgres
```

Container name:

```text
spendly-postgres
```

Current image:

```text
postgres:17
```

Default local configuration:

```text
Database: spendly
User: spendly
Password: spendly_password
Host port: 5432
Container port: 5432
```

The password is intended only for local development.

It must not be reused in production, staging, shared testing environments, or
publicly accessible database instances.

## Requirements

To use the Compose configuration, install a recent version of Docker Desktop or
Docker Engine with the Docker Compose plugin.

Docker is currently optional for Spendly development because the backend does
not connect to PostgreSQL yet.

## Start PostgreSQL

Run from the repository root:

```bash
docker compose -f deploy/docker-compose.yml up -d
```

The `-d` option starts the container in the background.

## Check container state

```bash
docker compose -f deploy/docker-compose.yml ps
```

## View PostgreSQL logs

```bash
docker compose -f deploy/docker-compose.yml logs -f spendly-postgres
```

Press `Ctrl+C` to stop following the logs. The container itself will continue
running in the background.

## Stop the container

```bash
docker compose -f deploy/docker-compose.yml stop
```

This stops the service without deleting the container or database volume.

## Stop and remove the container

```bash
docker compose -f deploy/docker-compose.yml down
```

This removes the Compose container and network but preserves the named database
volume.

## Remove the database volume

```bash
docker compose -f deploy/docker-compose.yml down --volumes
```

This command permanently deletes all data stored in the local PostgreSQL
volume.

Use it only when the local database can be safely recreated.

## Current volume

PostgreSQL data is stored in the named Docker volume:

```text
spendly_postgres_data
```

It is mounted inside the container at:

```text
/var/lib/postgresql/data
```

This allows local data to survive ordinary container recreation.

## Current backend integration status

PostgreSQL is not connected to the current Spendly backend.

The solution currently has:

- no Entity Framework Core packages;
- no `DbContext`;
- no entity configurations;
- no migrations;
- no repository implementations;
- no active database connection string;
- no PostgreSQL health check;
- no database-backed integration tests;
- no Testcontainers setup.

The following API configuration value is currently only a placeholder:

```text
Infrastructure:Database:Provider
```

Its current value is:

```text
NotConfigured
```

It documents the expected configuration structure but does not create a
database connection.

## When Docker is not required

Starting PostgreSQL is currently not required to:

- restore backend packages;
- build the solution;
- run unit tests;
- run integration tests;
- start the API;
- start the Worker;
- use OpenAPI or Scalar;
- call the current health check endpoints.

The current integration tests host the API in memory and do not use external
services.

## Future persistence milestone

When persistence is implemented, the Infrastructure project is expected to
receive:

- Entity Framework Core packages;
- the PostgreSQL EF Core provider;
- a `DbContext`;
- entity type configurations;
- repository implementations;
- database migrations;
- validated connection settings;
- database readiness checks;
- database-backed integration tests.

The Domain project must remain independent from those implementation details.

## Secrets and production environments

Production credentials must not be committed to the repository.

Future environment-specific secrets should be supplied through an appropriate
mechanism such as:

- development user secrets;
- environment variables;
- CI/CD secret storage;
- a production secret manager.

The values in `docker-compose.yml` are suitable only for isolated local
development.
