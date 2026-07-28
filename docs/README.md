# Spendly Documentation

This directory contains the product, architecture, and architectural decision
documentation for Spendly.

## Architecture

- [Architecture overview](architecture/overview.md) — projects, dependency
  direction, runtime flow, and current implementation boundaries.
- [Domain model](architecture/domain-model.md) — entities, value objects,
  identifiers, errors, and business invariants.
- [Persistence architecture](architecture/persistence.md) — production EF Core
  configuration, PostgreSQL storage rules, migrations, readiness, secrets, and
  database testing.
- [EF Core domain model compatibility](architecture/ef-core-domain-model-compatibility.md)
  — historical compatibility evidence and how it was promoted into the
  production persistence layer.

## Architectural decisions

- [ADR 0001: Use modular monolith](adr/0001-use-modular-monolith.md)
- [ADR 0002: Defer DateRange until required](adr/0002-defer-date-range-until-required.md)
- [ADR 0003: Define domain model persistence strategy](adr/0003-define-domain-model-persistence-strategy.md)

Architectural Decision Records preserve the context and rationale of important
technical or domain decisions. A later implementation update should be recorded
without rewriting the original decision as though it had always existed.

## Product

- [Product vision](product/vision.md)
- [MVP scope](product/mvp-scope.md)

Product documents describe what Spendly should provide to users. Architecture
documents describe how the software is organized and operated.

## Additional documentation

- [Repository overview](../README.md)
- [Backend development guide](../backend/README.md)
- [Domain project](../backend/src/Spendly.Domain/README.md)
- [Unit tests](../backend/tests/Spendly.UnitTests/README.md)
- [Integration tests](../backend/tests/Spendly.IntegrationTests/README.md)
- [Local infrastructure](../deploy/README.md)
