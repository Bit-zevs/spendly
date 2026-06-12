# ADR 0001: Use modular monolith

## Status

Accepted

## Context

Spendly is a long-term pet product developed by one developer. The project needs clear architecture without microservice complexity.

## Decision

Use a modular monolith with Clean Architecture Lite.

## Consequences

- Easier local development.
- Simpler deployment.
- Clear module boundaries.
- Possible extraction into separate services later if needed.
