# Architecture Overview

Spendly backend starts as a modular monolith with Clean Architecture Lite.

## Projects

- Spendly.Domain — domain entities, value objects and domain rules.
- Spendly.Application — use cases, commands, queries, validators and interfaces.
- Spendly.Infrastructure — database, external integrations and infrastructure implementations.
- Spendly.Api — HTTP API.
- Spendly.Worker — background jobs.
- Spendly.UnitTests — unit tests.
- Spendly.IntegrationTests — API and infrastructure integration tests.

## Dependency rule

- Domain depends on nothing.
- Application depends on Domain.
- Infrastructure depends on Application and Domain.
- Api depends on Application and Infrastructure.
- Worker depends on Application and Infrastructure.
