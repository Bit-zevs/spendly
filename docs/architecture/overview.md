# Architecture Overview

Spendly is developed as a modular monolith with Clean Architecture Lite.

The current structure creates explicit dependency boundaries without adding the
operational complexity of microservices. The product is still deployed as a
small set of hosts that share one codebase and one PostgreSQL database.

## Goals

The architecture provides:

- a Domain model independent from frameworks;
- clear ownership of business rules;
- testable Application behavior;
- replaceable Infrastructure implementations;
- thin HTTP and background-processing hosts;
- explicit PostgreSQL schema and deployment rules;
- a simple local development model;
- room for internal modules as product behavior grows.

## Current milestone

```text
v0.4 Persistence Layer
```

The repository currently has:

- backend hosting and observability foundations;
- the first Domain model;
- production EF Core and PostgreSQL persistence;
- the initial database migration;
- database readiness checking;
- real PostgreSQL integration tests.

Application use cases, repositories, and domain feature endpoints are not
implemented yet.

## Backend projects

### Spendly.Domain

The innermost project contains:

- entities;
- value objects;
- strongly typed identifiers;
- domain invariants;
- domain errors;
- business behavior that does not require external systems.

It has no references to other Spendly projects and contains no EF Core,
PostgreSQL, HTTP, logging, or serialization dependencies.

### Spendly.Application

The Application project will contain use cases and define contracts required
from outer systems.

Expected contents include:

- commands and queries;
- handlers and application services;
- validators;
- authorization decisions;
- domain-specific persistence ports;
- ports for clocks, external systems, and messaging;
- read projections independent from HTTP.

Application may depend on Domain, but not on EF Core implementation details,
ASP.NET Core endpoints, or concrete infrastructure services.

No production use cases are implemented yet.

### Spendly.Infrastructure

Infrastructure contains the implemented technical persistence layer:

- `SpendlyDbContext`;
- EF Core configurations;
- converters for Domain identifiers, currencies, and enums;
- PostgreSQL connection options and validation;
- one shared `NpgsqlDataSource`;
- EF Core migrations;
- PostgreSQL readiness checking.

Future technical implementations may include repositories for real Application
ports, external API clients, messaging, caching, file storage, and clocks.

### Spendly.Api

The HTTP host currently provides:

- ASP.NET Core startup;
- dependency registration;
- validated strongly typed options;
- Serilog logging and request logging;
- centralized exception handling;
- ProblemDetails responses;
- root status endpoint;
- liveness and readiness endpoints;
- OpenAPI and Scalar.

Future feature endpoints must translate HTTP contracts and delegate work to
Application use cases. They must not implement Domain business rules directly.

### Spendly.Worker

The background-processing host currently starts and waits for shutdown without
scheduled financial jobs.

Future jobs must call Application use cases and must not duplicate business
rules from Domain.

### Test projects

`Spendly.UnitTests` verifies Domain and future Application behavior without
infrastructure.

`Spendly.IntegrationTests` verifies:

- the configured API host;
- EF Core production model metadata;
- PostgreSQL migrations and physical schema;
- Domain persistence round trips;
- relationship behavior;
- PostgreSQL readiness.

## Dependency direction

Conceptual dependency direction is inward:

```text
┌───────────────────────────────────────────────┐
│ Spendly.Api              Spendly.Worker       │
│ Delivery mechanisms                           │
├───────────────────────────────────────────────┤
│ Spendly.Infrastructure                        │
│ Technical implementations                     │
├───────────────────────────────────────────────┤
│ Spendly.Application                           │
│ Use cases and external contracts              │
├───────────────────────────────────────────────┤
│ Spendly.Domain                                │
│ Business model and invariants                 │
└───────────────────────────────────────────────┘
```

Compile-time project references:

```text
Spendly.Domain
  └── no Spendly project references

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

Infrastructure currently references Application even though no persistence
ports exist yet. That preserves the intended dependency direction for future
implementations without allowing Application to depend on EF Core.

## Why Domain is independent

Domain defines valid Spendly business state. Its rules should produce the same
result regardless of whether a use case is invoked through:

- an HTTP API;
- a background worker;
- a web or mobile client;
- a Telegram bot;
- a command-line tool;
- a unit test.

The same model should remain valid if EF Core or PostgreSQL is replaced.

Domain therefore does not know about:

- controllers or minimal API endpoints;
- HTTP status codes or ProblemDetails;
- JSON contracts;
- EF Core attributes or `DbContext`;
- database tables or SQL;
- logging implementations;
- external clients.

Infrastructure may use private constructors, backing fields, complex
properties, and converters to persist Domain objects without introducing those
concerns into Domain.

## Current runtime flows

### Technical API endpoint

The API has no domain feature use cases yet. Its current general request flow is:

```text
HTTP request
    ↓
Spendly.Api endpoint or middleware
    ↓
configured response
```

### Readiness endpoint

Readiness crosses the Infrastructure boundary:

```text
GET /health/ready
    ↓
Spendly.Api health endpoint
    ↓
HealthCheckService
    ↓
Spendly.Infrastructure PostgreSqlHealthCheck
    ↓
NpgsqlDataSource
    ↓
PostgreSQL SELECT 1
```

The readiness path checks connectivity only. It does not apply migrations or
change schema state.

### Future feature request

After Application use cases are introduced, the intended flow is:

```text
HTTP request
    ↓
Spendly.Api endpoint
    ↓
Spendly.Application use case
    ↓
Spendly.Domain model
    ↓
Application persistence or external-system port
    ↓
Spendly.Infrastructure implementation
    ↓
PostgreSQL or another dependency
    ↓
HTTP response
```

The Worker will reuse the same Application and Domain layers without involving
HTTP.

## Current Domain model

The Domain model introduced in v0.3 contains:

- `Entity<TId>`;
- `ValueObject`;
- `IStronglyTypedId<TValue>`;
- `DomainError`, `DomainException`, and `DomainErrors`;
- `Currency` and `Money`;
- `Wallet`, `Category`, and `Transaction`;
- strongly typed identifiers and supporting enums.

See [Domain Model](domain-model.md).

## Current persistence boundary

Production persistence is implemented in `Spendly.Infrastructure`.

The context is:

```text
backend/src/Spendly.Infrastructure/Persistence/SpendlyDbContext.cs
```

The layer includes:

- explicit PostgreSQL mappings;
- strongly typed ID converters;
- `Currency` and `Money` mapping;
- enum and UTC timestamp mapping;
- restrictive transaction foreign keys;
- explicit check constraints and indexes;
- migration history;
- startup connection validation;
- a readiness health check.

The API registers the persistence infrastructure, but normal technical
endpoints do not create Domain data because Application use cases do not exist.

Repositories are intentionally deferred. They will be defined as focused
Application ports when concrete use cases require them. A generic CRUD
repository is not used.

See [Persistence Architecture](persistence.md).

## Migration boundary

API and Worker startup do not call `Database.Migrate()` or
`Database.MigrateAsync()`.

Schema updates are explicit deployment operations. This avoids DDL races
between replicas, keeps runtime database permissions narrow, and requires
review of destructive or data-rewriting changes.

Integration tests apply production migrations because migration execution is
the behavior being verified.

## Current API boundary

The API exposes technical foundation endpoints only:

- root status;
- liveness;
- readiness;
- OpenAPI;
- Scalar.

Wallet, category, transaction, authentication, budget, and reporting endpoints
remain deferred until their Application use cases exist.

## Modular monolith evolution

Spendly currently has project-level architecture boundaries rather than
separate deployable services.

As real behavior grows, internal modules may emerge, for example:

- Accounts;
- Finance;
- Budgeting;
- Subscriptions;
- Goals;
- Reporting;
- Notifications.

A module is introduced when enough behavior exists to define a meaningful
boundary. Empty modules and speculative abstractions are not added in advance.

## Architectural decisions

Important decisions are stored in:

```text
docs/adr
```

Current decisions:

- use a modular monolith;
- defer a generic `DateRange` until a real use case defines its semantics;
- use explicit EF Core and PostgreSQL persistence rules with controlled
  migrations and domain-specific persistence ports.
