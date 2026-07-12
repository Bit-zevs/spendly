# Spendly.Domain

`Spendly.Domain` contains the core business model of Spendly.

It is the innermost backend project and has no dependencies on other Spendly
projects.

## Responsibilities

The Domain project owns:

- entities;
- value objects;
- strongly typed identifiers;
- domain invariants;
- domain errors;
- business behavior independent from frameworks and external systems.

A domain object should protect its own valid state. Once an entity or value
object has been created successfully, outer layers should not need to repeat its
business validation.

## Dependency rule

`Spendly.Domain` must not reference:

- `Spendly.Api`;
- `Spendly.Application`;
- `Spendly.Infrastructure`;
- `Spendly.Worker`;
- ASP.NET Core;
- Entity Framework Core;
- database providers;
- HTTP contracts;
- serialization contracts;
- logging implementations.

Domain objects should remain usable from API requests, background jobs, unit
tests, and future user interfaces without changing their business rules.

## Current structure

```text
Spendly.Domain/
├── Categories/
├── Common/
├── Errors/
├── Transactions/
├── ValueObjects/
├── Wallets/
├── README.md
└── Spendly.Domain.csproj
```

## Common

The `Common` directory contains shared domain foundations:

- `Entity<TId>`;
- `ValueObject`;
- `IStronglyTypedId<TValue>`.

### Entity<TId>

`Entity<TId>` is the base class for domain objects with stable identity.

Two entities are equal when:

- they have the same concrete runtime type;
- they have the same identifier.

Their remaining properties do not define entity identity.

### ValueObject

`ValueObject` is the base class for immutable domain objects identified by their
values.

Derived value objects provide their ordered equality components through
`GetEqualityComponents()`.

The base class implements:

- `Equals`;
- `GetHashCode`;
- `==`;
- `!=`.

### Strongly typed identifiers

Current identifiers are:

- `WalletId`;
- `CategoryId`;
- `TransactionId`.

They wrap `Guid` while remaining incompatible with each other at compile time.
This prevents accidentally passing a category identifier where a wallet
identifier is expected.

New identifiers are generated through `Guid.CreateVersion7()`.

## Errors

The `Errors` directory contains:

- `DomainError`;
- `DomainException`;
- `DomainErrors`.

`DomainError` contains a stable machine-readable code and a human-readable
message.

`DomainException` reports a violated domain invariant and preserves its
corresponding `DomainError`.

`DomainErrors` is the centralized catalog of known business rule violations.

Domain errors contain no HTTP status codes, ProblemDetails objects, database
errors, or UI-specific information.

## Value objects

Current value objects are:

- `Currency`;
- `Money`.

### Currency

`Currency` represents a normalized three-letter currency code.

It validates the format, normalizes the code, and provides value equality.

### Money

`Money` represents a decimal amount together with its currency.

It protects monetary invariants and provides:

- zero and positive creation methods;
- a persistence-aligned `decimal(19, 4)` policy;
- a maximum amount of `999999999999999.9999`;
- rejection of values with more than four fractional digits;
- same-currency addition and subtraction;
- same-currency comparison;
- value equality;
- invariant formatting.

Automatic currency conversion is intentionally not implemented.

## Wallets

The wallet domain area contains:

- `Wallet`;
- `WalletId`;
- `WalletType`.

A wallet currently has:

- a generated identifier;
- a required normalized name limited to `Wallet.MaxNameLength`;
- a supported wallet type;
- a required currency;
- a UTC creation timestamp.

Wallet balance behavior, ownership, persistence, updates, and API contracts are
not implemented yet.

## Categories

The category domain area contains:

- `Category`;
- `CategoryId`;
- `CategoryType`.

A category currently has:

- a generated identifier;
- a required normalized name limited to `Category.MaxNameLength`;
- either the `Income` or `Expense` type;
- a UTC creation timestamp.

Icons, colors, hierarchy, budget limits, ownership, persistence, updates, and
API contracts are not implemented yet.

## Transactions

The transaction domain area contains:

- `Transaction`;
- `TransactionId`;
- `TransactionType`.

The current entity supports:

- income transactions;
- expense transactions.

A transaction requires:

- a supported transaction type;
- a positive monetary amount;
- a wallet whose currency matches the amount currency;
- a category matching the transaction direction;
- an occurrence timestamp;
- a creation timestamp.

Descriptions are optional, normalized before storage, and limited to
`Transaction.MaxDescriptionLength`.

`Transfer` is a known transaction type but is intentionally rejected by the
current transaction factory. A correct transfer model requires at least source
and destination wallets and may also require separate amounts, an exchange
rate, and fees.

## Creation and invariants

Current entities are created through controlled static factory methods:

```csharp
Wallet.Create(...);
Category.Create(...);
Transaction.Create(...);
```

Their constructors are private and their public properties are read-only.

This prevents application and API code from constructing entities through
arbitrary property assignment and bypassing business invariants.

Future changes to entity state should be exposed through explicit domain
methods that preserve those invariants.

## Date and time handling

Current entity timestamps use `DateTimeOffset`.

Non-default timestamps are converted to UTC through `ToUniversalTime()` before
being stored.

Calendar-only types such as `DateOnly` should be introduced only for concepts
that are genuinely calendar based.

## DateRange decision

A generic `DateRange` value object is intentionally not implemented in v0.3.

No current entity requires a reusable date interval. Its design is deferred
until a real budget, report, goal, subscription, or transaction-filtering use
case defines:

- whether it uses `DateOnly` or `DateTimeOffset`;
- whether its boundaries are inclusive;
- whether the end value is mandatory;
- whether open-ended periods are supported;
- which operations are required;
- whether one generic type is suitable for multiple domain areas.

See
[ADR 0002](../../../docs/adr/0002-defer-date-range-until-required.md).

## Current boundaries

The Domain project does not contain:

- application commands or queries;
- use-case handlers;
- repositories;
- Entity Framework Core configurations;
- database attributes;
- migrations;
- API endpoints;
- request or response DTOs;
- HTTP status codes;
- ProblemDetails;
- authentication implementation.

These concerns belong to outer projects.

## Documentation

Complete domain model and business rules:

- [Domain model](../../../docs/architecture/domain-model.md)

Architectural decision about date ranges:

- [ADR 0002: Defer DateRange until required](../../../docs/adr/0002-defer-date-range-until-required.md)

Folder-specific documentation:

- [Common](Common/README.md)
- [Errors](Errors/README.md)
- [Value objects](ValueObjects/README.md)
- [Wallets](Wallets/README.md)
- [Categories](Categories/README.md)
- [Transactions](Transactions/README.md)
