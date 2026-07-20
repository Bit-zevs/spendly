# Domain Model

This document describes the domain model implemented in Spendly v0.3.

## Domain model at a glance

The current domain model consists of:

- shared foundations: `Entity<TId>`, `ValueObject`, and
  `IStronglyTypedId<TValue>`;
- domain errors: `DomainError`, `DomainException`, and `DomainErrors`;
- value objects: `Currency` and `Money`;
- entities: `Wallet`, `Category`, and `Transaction`;
- strongly typed identifiers for each entity;
- enums describing wallet, category, and transaction types.

The domain model currently contains business rules only. It is not connected
to Application use cases, Entity Framework Core, PostgreSQL, or domain HTTP
endpoints yet.

## Purpose of Spendly.Domain

`Spendly.Domain` contains the business concepts and invariants that define valid
Spendly state.

The project answers questions such as:

- what is a wallet;
- what values form a monetary amount;
- what makes a category valid;
- what makes a transaction valid;
- which transaction and category combinations are allowed;
- how domain objects are compared;
- how business rule violations are represented.

The project does not decide:

- how objects are stored;
- how HTTP requests are parsed;
- which HTTP status code is returned;
- how JSON is serialized;
- how a user is authenticated;
- how a background job is scheduled.

Those concerns belong to outer layers.

## Domain independence

`Spendly.Domain.csproj` has no project references.

It intentionally does not depend on:

- `Spendly.Api`;
- `Spendly.Application`;
- `Spendly.Infrastructure`;
- `Spendly.Worker`;
- ASP.NET Core;
- Entity Framework Core;
- PostgreSQL;
- HTTP contracts;
- logging providers.

This keeps business rules usable from API requests, background jobs, tests, and
future user interfaces without changing the domain model.

## Shared domain building blocks

### Entity<TId>

`Entity<TId>` is the base type for objects with stable identity.

Two entities are equal when:

- they have the same concrete runtime type;
- they have the same identifier.

Their other property values do not define entity identity.

The base class rejects the default identifier value when an entity is
constructed. It intentionally has no parameterless constructor, so every
derived entity must provide a valid identifier to the base class.

### ValueObject

`ValueObject` is the base type for immutable objects identified by their values.

Two value objects are equal when:

- they have the same concrete runtime type;
- all ordered equality components are equal.

Derived value objects provide their components through
`GetEqualityComponents()`.

The base class implements:

- `Equals`;
- `GetHashCode`;
- `==`;
- `!=`.

### Strongly typed identifiers

`WalletId`, `CategoryId`, and `TransactionId` are separate types even though
they all wrap `Guid`.

This prevents accidental identifier mixing such as passing a `CategoryId` where
a `WalletId` is required.

Each identifier:

- implements `IStronglyTypedId<Guid>`;
- rejects `Guid.Empty` when created through its public constructor or `From`;
- creates new values through `Guid.CreateVersion7()`;
- can restore an existing value through `From`;
- formats itself using the standard hyphenated GUID representation.

Version 7 UUIDs are time-ordered, which is useful for future database indexing,
while the strongly typed wrapper preserves domain type safety.

Because these identifiers are record structs, C# can still produce their
default value without invoking the constructor. That default value is invalid
and must be rejected by domain entry points that accept an identifier.

## Domain error model

### DomainError

`DomainError` is an immutable value containing:

- a stable machine-readable `Code`;
- a human-readable `Message`.

Both values are required.

Example:

```text
Transaction.Category.TypeMismatch
```

The code is suitable for tests, logging, future API mapping, and client-side
handling.

### DomainException

`DomainException` represents a violated domain invariant.

It contains the original `DomainError` and exposes its code through the `Code`
property.

The exception itself does not contain:

- an HTTP status code;
- ProblemDetails;
- an API response;
- persistence-specific information.

Translation into an HTTP or UI representation belongs to an outer layer.

### DomainErrors

`DomainErrors` is the centralized catalog of known business rule violations.

It currently contains errors for:

- Currency;
- Money;
- Wallet;
- Category;
- Transaction.

Stable error codes allow tests and future clients to rely on a defined contract
instead of comparing exception text.

## Value objects

### Currency

`Currency` represents a normalized three-letter currency code.

#### Why Currency exists

`Currency` keeps the monetary unit explicit and prevents arbitrary,
differently formatted strings from spreading through the domain model.

It centralizes currency normalization, validation, and value equality, so
values such as `usd`, `USD`, and ` USD ` represent the same currency.

Using a dedicated type also makes it harder to accidentally treat a random
string as a valid currency code.

#### Rules

A currency code:

- is required;
- is trimmed;
- is converted to uppercase using invariant casing;
- must contain exactly three characters;
- may contain only Latin letters from `A` to `Z`.

Examples:

```text
" usd " -> "USD"
"eur"   -> "EUR"
"RUB"   -> "RUB"
```

Invalid examples:

```text
""
"US"
"USDT"
"12A"
"РУБ"
```

#### Known instances

The class provides shared instances for:

- `USD`;
- `EUR`;
- `RUB`.

Other syntactically valid three-letter codes may still be created.

The current implementation validates the format of a currency code. It does not
maintain a complete ISO 4217 registry and does not reject an unknown but
well-formed code.

#### Equality

Two currencies are equal when their normalized codes are equal.

### Money

`Money` represents a decimal amount together with its currency.

An amount without a currency is not valid domain money.

#### Why Money exists

`Money` keeps an amount and its currency inseparable instead of passing a
standalone `decimal` through the application.

It centralizes monetary rules, prevents `100 USD` from being treated as the
same value as `100 EUR`, and rejects arithmetic or comparison between
different currencies.

Without this value object, every entity and use case would have to repeat
currency validation, amount validation, and same-currency checks.

#### Rules

- currency is required;
- amount cannot be negative;
- amount cannot exceed `Money.MaxAmount`;
- amount can contain at most `Money.Scale` fractional digits;
- the persistence policy is `decimal(19, 4)`;
- `From` allows zero or a positive amount;
- `Positive` requires an amount greater than zero;
- `Zero` creates a zero amount for a required currency.

Examples:

```csharp
Money.From(100m, Currency.Usd);
Money.Positive(25.50m, Currency.Eur);
Money.Zero(Currency.Rub);
```

#### Persistence materialization

`Money` has a private parameterless constructor reserved for persistence
materialization. Application code cannot call it and must continue to create
instances through `From`, `Positive`, or `Zero`, so normal domain creation
still enforces monetary invariants.

#### Equality

Two money values are equal when both values match:

- amount;
- currency.

Therefore:

```text
10 USD == 10 USD
10 USD != 10 EUR
10 USD != 20 USD
```

#### Arithmetic

Money supports:

- `Add`;
- `Subtract`;
- `+`;
- `-`.

Arithmetic requires matching currencies.

Adding or subtracting different currencies produces
`Money.Currency.Mismatch`.

The result must also satisfy the non-negative amount invariant. A subtraction
that would produce a negative value is rejected.

Currency conversion is intentionally not performed automatically. A future
exchange operation will require an explicit exchange rate and its own business
rules.

#### Comparison

Money implements `IComparable<Money>` and supports:

- `>`;
- `<`;
- `>=`;
- `<=`.

Values can be compared only when their currencies match.

#### Formatting

`Money` implements `IFormattable`.

Its default representation uses invariant culture and includes the currency
code:

```text
100.50 USD
```

## Entities

### Wallet

`Wallet` represents a place where the user keeps or tracks money.

Current wallet properties:

- `Id`;
- `Name`;
- `Type`;
- `Currency`;
- `CreatedAt`.

#### Wallet rules

A wallet:

- receives a newly generated `WalletId`;
- must have a non-empty name;
- trims its name before storing it;
- limits the normalized name to `Wallet.MaxNameLength`;
- must use a defined `WalletType`;
- must have a currency;
- must have a non-default creation timestamp;
- stores its creation timestamp in UTC.

#### Wallet types

Current supported values:

- `Cash`;
- `DebitCard`;
- `CreditCard`;
- `BankAccount`;
- `Savings`;
- `Investment`;
- `Other`.

The numeric value `0` is intentionally not assigned. Default and otherwise
undefined enum values are rejected.

#### Current limitations

Wallet currently does not contain:

- a persisted balance;
- an opening balance;
- ownership by a user;
- update operations;
- archive or deletion state;
- production EF Core mappings;
- API contracts.

A future balance should be derived or updated through defined transaction rules,
not introduced as an unprotected mutable number.

### Category

`Category` classifies an income or expense transaction.

Current properties:

- `Id`;
- `Name`;
- `Type`;
- `CreatedAt`.

#### Category rules

A category:

- receives a newly generated `CategoryId`;
- must have a non-empty name;
- trims its name before storing it;
- limits the normalized name to `Category.MaxNameLength`;
- must use a defined `CategoryType`;
- must have a non-default creation timestamp;
- stores its creation timestamp in UTC.

#### Category types

Current supported values:

- `Income`;
- `Expense`.

The type describes financial direction. Specific names such as groceries,
salary, transport, health, and subscriptions are category instances, not enum
values.

The numeric value `0` is intentionally not assigned and is rejected.

#### Current limitations

Category currently does not contain:

- icons;
- colors;
- parent-child hierarchy;
- budget limits;
- ownership by a user;
- update operations;
- production EF Core mappings;
- API contracts.

### Transaction

`Transaction` represents a recorded money movement.

Current properties:

- `Id`;
- `Type`;
- `Amount`;
- `WalletId`;
- `CategoryId`;
- `OccurredAt`;
- `Description`;
- `CreatedAt`;
- `UpdatedAt`.

`UpdatedAt` is part of the initial model and is `null` for a newly created
transaction. A future transaction-editing use case will introduce explicit
domain methods that set it consistently.

#### Transaction types

The enum defines:

- `Income`;
- `Expense`;
- `Transfer`.

The current entity supports creating:

- income transactions;
- expense transactions.

Creation receives the complete `Wallet` aggregate reference, verifies that the
transaction amount uses the same currency, and stores only `WalletId` after the
invariant has been checked.

`Transfer` is recognized as a future transaction kind but is intentionally
rejected by the current factory.

A correct transfer model requires at least:

- a source wallet;
- a destination wallet.

A multi-currency transfer may additionally require:

- source amount;
- destination amount;
- exchange rate;
- fees.

These rules should be introduced together instead of representing a transfer as
an incomplete one-wallet transaction.

#### Transaction rules

A currently supported transaction:

- receives a newly generated `TransactionId`;
- must use a defined transaction type;
- must not be a transfer;
- must have a `Money` amount;
- must have an amount greater than zero;
- must have a non-default `WalletId`;
- must have a category;
- must use an income category for income;
- must use an expense category for expense;
- must have a non-default occurrence timestamp;
- must have a non-default creation timestamp;
- stores occurrence and creation timestamps in UTC;
- initializes `UpdatedAt` with `null`;
- may have an optional description;
- trims a meaningful description;
- converts an empty or whitespace-only description to `null`;
- limits description length to 500 characters after trimming.

Amounts are always positive. Financial direction is represented by
`TransactionType`, not by a negative amount.

#### Category validation

`Transaction.Create` accepts a `Category` entity rather than an independent
category identifier and category type.

This allows the transaction to verify the actual category type and prevents a
caller from providing contradictory values.

After validation, the transaction stores the category identifier.

#### Current limitations

Transaction currently does not implement:

- wallet balance changes;
- transfers;
- editing;
- deletion;
- refunds;
- cashback;
- correction transactions;
- split transactions;
- recurring transactions;
- persistence mapping;
- API contracts.

## Immutability

The current entities expose read-only public properties and use private
constructors with controlled factory methods.

This prevents callers from creating obviously invalid objects through arbitrary
property assignment.

Future state-changing behavior should be introduced through explicit domain
methods that preserve invariants.

## Date and time handling

Current entity timestamps use `DateTimeOffset`.

Incoming non-default values are normalized through `ToUniversalTime()` before
being stored.

This ensures that the domain stores a consistent UTC representation while
retaining an explicit offset-aware input type.

Calendar-only concepts may use `DateOnly` in future when a real use case
requires them.

## DateRange decision

A generic `DateRange` value object is intentionally not part of v0.3.

Current entities contain individual timestamps, but none requires a period.

Creating a reusable range now would require guessing:

- `DateOnly` versus `DateTimeOffset`;
- inclusive versus exclusive end boundary;
- whether an end date is mandatory;
- whether a one-day range is valid;
- whether goals and subscriptions can be open-ended;
- whether budgets and reports share the same semantics;
- which methods are required.

The type will be reconsidered with the first real domain feature that needs a
range, most likely:

- a budget period;
- a report period;
- a goal period;
- a subscription period;
- a transaction history filter.

See
[ADR 0002](../adr/0002-defer-date-range-until-required.md).

## Application and persistence status

The v0.3 domain model is not connected to application use cases yet.

There are currently no:

- create-wallet commands;
- transaction handlers;
- category queries;
- repositories;
- production EF Core configurations;
- database tables;
- domain API endpoints.

This is intentional. The domain model is implemented and tested before adding
application orchestration and production persistence. A test-only compatibility
spike already verifies EF Core and PostgreSQL materialization; its findings are
documented in
[EF Core Domain Model Compatibility](ef-core-domain-model-compatibility.md).

The future storage contract is accepted in
[ADR 0003: Define domain model persistence strategy](../adr/0003-define-domain-model-persistence-strategy.md).
It does not add persistence concerns to the Domain project.

## Testing

Domain unit tests cover:

- shared entity behavior;
- value object equality;
- strongly typed identifiers;
- domain errors;
- domain exceptions;
- Currency;
- Money;
- Wallet;
- WalletType;
- Category;
- CategoryType;
- Transaction;
- TransactionType.

Tests verify successful creation, normalization, equality, formatting,
arithmetic, comparison, invalid values, and business rule violations.
