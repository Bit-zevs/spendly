# Spendly Documentation

This directory contains product, architecture, operational, and architectural
decision documentation for Spendly.

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
technical or domain decisions. When a decision is implemented, superseded, or
reconsidered, update its status without rewriting its original context as
though it had always described the final implementation.

## Product

- [Product delivery status](product/delivery-status.md) — the source of truth
  for what is actually implemented now.
- [MVP scope](product/mvp-scope.md) — the agreed target boundary of the first
  useful product release.
- [Product vision](product/vision.md) — the long-term user value and possible
  product directions.

Product vision and MVP scope describe intended outcomes. They do not prove that
a feature is already available. Current implementation progress belongs in the
product delivery status and the relevant architecture or operational document.

## Additional documentation

- [Repository overview](../README.md)
- [Backend development guide](../backend/README.md)
- [Domain project](../backend/src/Spendly.Domain/README.md)
- [Unit tests](../backend/tests/Spendly.UnitTests/README.md)
- [Integration tests](../backend/tests/Spendly.IntegrationTests/README.md)
- [Local infrastructure](../deploy/README.md)

## Documentation maintenance

When production behavior changes, update in the same pull request:

- the product delivery status when a user-facing capability advances;
- the repository or backend overview when the current milestone changes;
- architecture documents when boundaries, mappings, runtime flows, or
  operational rules change;
- command examples when project paths, tooling, configuration keys, or launch
  profiles change;
- the relevant ADR implementation status when an accepted decision is
  implemented or superseded.

Do not replace historical ADR context with current-state prose. Record the new
implementation status or add a focused follow-up ADR instead.
