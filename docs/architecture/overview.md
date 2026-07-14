# Architecture Overview

Spendly is being developed as a modular monolith with Clean Architecture Lite.

The current structure provides explicit dependency boundaries without adding
the operational complexity of microservices.

## Goals

The architecture is intended to provide:

- a domain model independent from frameworks;
- clear ownership of business rules;
- testable application behavior;
- replaceable infrastructure implementations;
- thin HTTP and background-processing hosts;
- a simple local development and deployment model;
- the ability to introduce internal modules as the product grows.

## Backend projects

### Spendly.Domain

The innermost project.

It contains:

- entities;
- value objects;
- strongly typed identifiers;
- domain invariants;
- domain errors;
- business behavior that does not require external systems.

It must remain pure C# and must not reference other Spendly projects.

### Spendly.Application

Contains application-specific use cases.

It will coordinate domain objects and define contracts required from external
systems.

Expected contents include:

- commands;
- queries;
- handlers;
- application services;
- validators;
- repository interfaces;
- transaction boundaries;
- authorization decisions;
- ports for infrastructure services.

Application may depend on Domain but must not depend on ASP.NET Core endpoints,
EF Core implementation details, or concrete external services.

No production use cases are implemented in this project yet.

### Spendly.Infrastructure

Contains technical implementations of contracts defined by inner layers.

Expected contents include:

- EF Core `DbContext`;
- entity configurations;
- PostgreSQL repositories;
- migrations;
- external API clients;
- messaging;
- caching;
- file storage;
- technical clock implementations.

Infrastructure currently contains project references only. Database access is
not implemented.

### Spendly.Api

The HTTP delivery mechanism.

It currently provides:

- ASP.NET Core application startup;
- dependency injection configuration;
- validated strongly typed options;
- Serilog logging;
- request logging;
- centralized exception handling;
- ProblemDetails responses;
- health checks;
- OpenAPI;
- Scalar;
- a root status endpoint.

Future feature endpoints must delegate work to Application use cases instead of
implementing business rules directly.

### Spendly.Worker

The background-processing delivery mechanism.

The current worker is a minimal host without scheduled domain jobs.

Future jobs must call Application use cases. The Worker must not duplicate
business rules from Domain.

### Test projects

`Spendly.UnitTests` verifies isolated Domain and future Application behavior.

`Spendly.IntegrationTests` verifies the configured API host and, in future
milestones, infrastructure integrations.

## Dependency direction

The conceptual dependency direction is inward:

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

## Why Domain is independent

Domain contains the rules that define valid Spendly business state.

These rules should produce the same result regardless of whether the
application is used through:

- an HTTP API;
- a background worker;
- a Telegram bot;
- a web application;
- a mobile application;
- a command-line tool;
- unit tests.

The same business rules should also remain valid if PostgreSQL or EF Core is
replaced.

For this reason Domain must not know about:

- controllers or minimal API endpoints;
- HTTP status codes;
- ProblemDetails;
- JSON contracts;
- EF Core attributes;
- `DbContext`;
- database tables;
- SQL;
- logging implementations;
- external service clients.

Outer layers may translate domain behavior into their own representation. For
example, Api may translate a domain error into a ProblemDetails response, but
Domain must not produce an HTTP response itself.

## Current request flow

The current API has no domain feature use cases yet.

Its present request flow is:

```text
HTTP request
    ↓
Spendly.Api endpoint or middleware
    ↓
configured response
```

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
Application contract
    ↓
Spendly.Infrastructure implementation
    ↓
HTTP response
```

The Worker will use the same Application and Domain layers without involving
HTTP.

## Current domain model

Version v0.3 currently contains:

- `Entity<TId>`;
- `ValueObject`;
- `IStronglyTypedId<TValue>`;
- `DomainError`;
- `DomainException`;
- `DomainErrors`;
- `Currency`;
- `Money`;
- `Wallet`;
- `Category`;
- `Transaction`;
- strongly typed identifiers and supporting enums.

See [Domain Model](domain-model.md) for complete rules.

## Current persistence boundary

PostgreSQL is available through the optional Docker Compose configuration.

The production projects do not yet contain:

- a production `DbContext`;
- migrations;
- repository implementations;
- a connection string used by the API;
- database health checks;
- application persistence use cases.

The future storage contract is accepted in
[ADR 0003: Define domain model persistence strategy](../adr/0003-define-domain-model-persistence-strategy.md).
It fixes PostgreSQL types, value conversions, enum storage, monetary precision,
UTC timestamp rules, naming conventions, restrictive foreign keys, migration
deployment policy, and Testcontainers-based database testing.

A database-backed compatibility spike exists in
`Spendly.IntegrationTests.Persistence.Compatibility`.

The spike uses EF Core, Npgsql, PostgreSQL, and Testcontainers to verify that
the immutable Domain model can be persisted and materialized without public
setters or EF Core attributes. Metadata-based tests also protect the accepted
storage contract without opening a database connection.

The spike context and configurations are test-only. Production mappings should
later be implemented in Infrastructure using ADR 0003, then covered by
migration and PostgreSQL round-trip tests before the temporary context is
removed.

See
[EF Core Domain Model Compatibility](ef-core-domain-model-compatibility.md)
for the complete findings.

## Current API boundary

The API currently exposes technical foundation endpoints only:

- root status;
- liveness;
- readiness;
- OpenAPI;
- Scalar.

Wallet, category, transaction, authentication, budget, and reporting endpoints
are intentionally deferred until their Application use cases exist.

## Modular monolith evolution

Spendly currently has project-level architecture boundaries rather than
separate deployable services.

As the product grows, domain capabilities may be organized into internal
modules such as:

- Accounts;
- Finance;
- Budgeting;
- Subscriptions;
- Goals;
- Reporting;
- Notifications.

A module should be introduced when there is enough real behavior to define a
meaningful boundary. Empty abstractions and speculative modules should not be
added in advance.

## Architectural decision records

Important decisions are stored in:

```text
docs/adr
```

Current decisions:

- use a modular monolith;
- defer a generic `DateRange` until a real use case defines its semantics.
