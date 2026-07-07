# Spendly.Domain

`Spendly.Domain` contains the core business model of Spendly.

This project is the innermost layer of the backend and must stay independent from
API, infrastructure, persistence, background workers, and delivery mechanisms.

## Responsibilities

The domain layer is responsible for:

- business entities;
- aggregate roots;
- value objects;
- domain errors;
- domain invariants;
- business rules that should not depend on frameworks.

## Dependency rule

`Spendly.Domain` must not reference:

- `Spendly.Api`;
- `Spendly.Application`;
- `Spendly.Infrastructure`;
- `Spendly.Worker`;
- Entity Framework Core;
- ASP.NET Core;
- database-specific packages;
- HTTP-specific contracts.

The domain should remain pure C# code.

## Current structure

- `Common` — shared domain building blocks.
- `Errors` — domain-level errors.
- `ValueObjects` — immutable value-based domain types.
- `Wallets` — wallet-related domain model.
- `Categories` — category-related domain model.
- `Transactions` — transaction-related domain model.
