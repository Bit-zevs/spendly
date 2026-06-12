# Architecture Overview

Spendly backend starts as a modular monolith with Clean Architecture Lite.

## Projects

- Spendly.Domain вЂ” domain entities, value objects and domain rules.
- Spendly.Application вЂ” use cases, commands, queries, validators and interfaces.
- Spendly.Infrastructure вЂ” database, external integrations and infrastructure implementations.
- Spendly.Api вЂ” HTTP API.
- Spendly.Worker вЂ” background jobs.
- Spendly.UnitTests вЂ” unit tests.
- Spendly.IntegrationTests вЂ” API and infrastructure integration tests.

## Dependency rule

- Domain depends on nothing.
- Application depends on Domain.
- Infrastructure depends on Application and Domain.
- Api depends on Application and Infrastructure.
- Worker depends on Application and Infrastructure.
