# Domain Unit Tests

This directory contains isolated tests for the Spendly domain model.

The tests verify domain behavior without starting the API, connecting to a
database, accessing the file system, or calling external services.

## Current structure

```text
Domain/
├── Categories/
├── Common/
├── Errors/
├── Transactions/
├── ValueObjects/
├── Wallets/
├── DomainProjectStructureTests.cs
└── README.md
```

## Current coverage

### Common

Tests verify:

- entity equality by concrete type and identifier;
- inequality for different identifiers;
- inequality for different concrete entity types;
- default entity identifier rejection;
- value object equality by ordered components;
- inequality for different value object types;
- strongly typed identifier generation;
- version 7 UUID generation;
- existing identifier restoration;
- empty identifier rejection.

### Errors

Tests verify:

- `DomainError` construction;
- required error codes and messages;
- value equality;
- string representation;
- `DomainException` error preservation;
- domain exception messages;
- stable error-code access;
- inner exception preservation;
- null error rejection;
- uniqueness of known domain error codes;
- non-empty messages for known domain errors.

### Currency

Tests verify:

- valid currency creation;
- trimming;
- invariant uppercase normalization;
- shared known currency instances;
- currency code length;
- value equality;
- invalid format rejection;
- null and whitespace rejection;
- string representation.

### Money

Tests verify:

- creation of zero and positive values;
- negative amount rejection;
- required currency;
- positive-only creation;
- value equality;
- same-currency detection;
- addition;
- subtraction;
- arithmetic operators;
- negative-result rejection;
- currency mismatch rejection;
- comparison;
- relational operators;
- null argument handling;
- decimal-based public API;
- decimal arithmetic behavior;
- invariant formatting;
- custom numeric formatting.

### Wallets

Tests verify:

- wallet creation;
- identifier generation;
- name normalization;
- supported wallet types;
- invalid wallet type rejection;
- required currency;
- creation timestamp validation;
- UTC timestamp normalization;
- stable wallet type numeric values;
- the exact set of supported wallet types;
- invalid default wallet type behavior.

### Categories

Tests verify:

- category creation;
- identifier generation;
- name normalization;
- supported category types;
- invalid category type rejection;
- creation timestamp validation;
- UTC timestamp normalization;
- stable category type numeric values;
- the exact set of category types;
- invalid default category type behavior.

### Transactions

Tests verify:

- income creation;
- expense creation;
- identifier generation;
- description normalization;
- maximum description length;
- validation after trimming;
- occurrence timestamp normalization;
- creation timestamp normalization;
- invalid transaction type rejection;
- unsupported transfer rejection;
- required monetary amount;
- positive amount requirement;
- required wallet;
- amount and wallet currency compatibility;
- required category;
- category and transaction type compatibility;
- default timestamp rejection;
- stable transaction type numeric values;
- the exact set of transaction types;
- invalid default transaction type behavior.

### Project structure

`DomainProjectStructureTests` verifies that:

- required domain directories exist;
- each domain directory contains its documentation;
- `Spendly.Domain` does not reference other Spendly projects.

These tests protect the current architectural boundary in addition to business
behavior.

## Testing principles

Domain unit tests should:

- verify observable business behavior;
- remain fast and deterministic;
- test both successful and invalid paths;
- assert stable domain error codes;
- avoid dependence on private implementation details;
- avoid ASP.NET Core hosting;
- avoid databases and file systems;
- avoid network calls;
- avoid external services;
- use descriptive test names.

Tests should verify the contract of a domain object rather than duplicate every
line of its implementation.

## Error assertions

Shared domain exception assertions are stored in:

```text
Spendly.UnitTests/TestUtilities
```

The current helper is:

```text
DomainExceptionAssert
```

Use it when it makes the expected domain error clearer and prevents repetitive
exception checks.

See [Test utilities](../TestUtilities/README.md).

## Adding new domain tests

Tests should be added together with the domain concept or business behavior they
protect.

A new domain feature normally requires tests for:

- successful creation or execution;
- boundary values;
- normalization;
- equality when applicable;
- every important invariant;
- stable domain error codes;
- unsupported or invalid combinations;
- null handling when the public API permits nullable input.

## Future areas

Potential future domain test areas include:

- wallet balance behavior;
- transfers;
- transaction editing;
- budgets;
- financial goals;
- subscriptions;
- recurring transactions;
- period value objects.

A generic `DateRange` test suite must not be added until a real domain use case
justifies the value object and defines its boundary semantics.

For the full domain description, see
[Domain model](../../../../docs/architecture/domain-model.md).
