# Value Objects

This directory contains immutable domain concepts identified by their values
rather than by a separate entity identity.

## Current value objects

The current domain model contains:

- `Currency`;
- `Money`.

Both types derive from the shared `ValueObject` base class.

## Value equality

A value object is equal to another value object when:

- both objects have the same concrete runtime type;
- all ordered equality components are equal.

Value objects do not require identifiers.

For example, two independently created `Money` instances representing the same
amount and currency are equal.

## Currency

`Currency` represents a normalized three-letter currency code.

### Rules

A currency code:

- is required;
- is trimmed before validation;
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

### Known currencies

Shared instances are provided for:

- `Currency.Usd`;
- `Currency.Eur`;
- `Currency.Rub`.

Calling `Currency.From` with one of these codes returns the corresponding shared
instance.

Other syntactically valid three-letter codes may also be created.

The current implementation validates code format. It does not maintain a
complete ISO 4217 currency registry.

### Equality

Currency equality is based on the normalized code.

Therefore:

```text
Currency.From("usd") == Currency.Usd
Currency.From("EUR") == Currency.From(" eur ")
Currency.Usd != Currency.Eur
```

## Money

`Money` represents a decimal amount in a specific currency.

An amount without a currency does not represent valid domain money.

### Creation methods

Available factory methods:

```csharp
Money.From(amount, currency);
Money.Positive(amount, currency);
Money.Zero(currency);
```

`Money.From`:

- requires a currency;
- accepts zero or a positive amount;
- rejects a negative amount.

`Money.Positive`:

- requires a currency;
- requires an amount greater than zero.

`Money.Zero`:

- requires a currency;
- creates an amount equal to zero.

### Equality

Money equality uses:

- `Amount`;
- `Currency`.

Therefore:

```text
10 USD == 10 USD
10 USD != 10 EUR
10 USD != 20 USD
```

### Arithmetic

Money supports:

- `Add`;
- `Subtract`;
- `+`;
- `-`.

Arithmetic requires matching currencies.

Operations between different currencies produce the
`Money.Currency.Mismatch` domain error.

A subtraction that would produce a negative result is rejected because every
`Money` instance must preserve the non-negative amount invariant.

### Comparison

Money implements `IComparable<Money>` and supports:

- `>`;
- `<`;
- `>=`;
- `<=`.

Comparison also requires matching currencies.

### Formatting

Money implements `IFormattable`.

Its default string representation uses invariant culture and includes the
currency code:

```text
100.50 USD
```

A custom numeric format and format provider may be supplied through the
`IFormattable` contract.

### Currency conversion

Money does not perform automatic currency conversion.

A future conversion operation must explicitly define:

- the source currency;
- the target currency;
- the exchange rate;
- exchange-rate precision;
- rounding rules;
- the effective date or time of the rate.

## DateRange status

`DateRange` is intentionally not implemented in v0.3.

The current domain model has no entity or use case that requires a reusable date
interval.

Its future design must first determine:

- whether it represents calendar dates or exact moments;
- whether it uses `DateOnly` or `DateTimeOffset`;
- whether the start and end boundaries are inclusive;
- whether an end value is mandatory;
- whether equal boundaries are valid;
- whether open-ended periods are allowed;
- which operations such as `Contains` or `Overlaps` are required;
- whether budgets, reports, goals, and subscriptions should share one type.

The decision is documented in
[ADR 0002](../../../../docs/adr/0002-defer-date-range-until-required.md).

The correct future abstraction may be either a generic `DateRange` or a more
specific type such as:

- `BudgetPeriod`;
- `ReportPeriod`;
- `GoalPeriod`;
- `SubscriptionPeriod`.

## Rules for future value objects

A value object added to this directory should:

- represent a real domain concept;
- be immutable;
- protect its invariants during creation;
- expose no invalid state;
- define every equality component;
- contain domain behavior when appropriate;
- remain independent from API and Infrastructure;
- use stable domain errors for business rule violations;
- have focused unit tests.

Do not introduce a value object only to wrap a primitive without adding domain
meaning, validation, type safety, equality semantics, or behavior.

For the complete description of the current domain model, see
[Domain model](../../../../docs/architecture/domain-model.md).
