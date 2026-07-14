# Spendly Documentation

This directory contains product, architecture, and architectural decision
documentation for Spendly.

## Architecture

- [Architecture overview](architecture/overview.md) — projects, dependency
  direction, runtime flow, and implementation boundaries.
- [Domain model](architecture/domain-model.md) — current v0.3 entities, value
  objects, identifiers, errors, and business invariants.
- [EF Core domain model compatibility](architecture/ef-core-domain-model-compatibility.md)
  — PostgreSQL materialization evidence and the executable compatibility model.

## Architectural decisions

- [ADR 0001: Use modular monolith](adr/0001-use-modular-monolith.md)
- [ADR 0002: Defer DateRange until required](adr/0002-defer-date-range-until-required.md)
- [ADR 0003: Define domain model persistence strategy](adr/0003-define-domain-model-persistence-strategy.md)

Architectural Decision Records explain why an important technical or domain
decision was made. They should preserve the original context even if the
decision is later superseded.

## Product

- [Product vision](product/vision.md)
- [MVP scope](product/mvp-scope.md)

Product documents describe what Spendly should provide to users.

Architecture documents describe how the software is organized.

## Additional documentation

- [Repository overview](../README.md)
- [Backend guide](../backend/README.md)
- [Domain project](../backend/src/Spendly.Domain/README.md)
- [Unit tests](../backend/tests/Spendly.UnitTests/README.md)
- [Integration tests](../backend/tests/Spendly.IntegrationTests/README.md)
- [Local infrastructure](../deploy/README.md)
