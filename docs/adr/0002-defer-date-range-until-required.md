# ADR 0002: Defer DateRange until required

## Status

Accepted

## Date

2026-07-12

## Context

Spendly may eventually need a date range for:

- budget periods;
- report periods;
- financial goal periods;
- subscription periods;
- transaction filters.

The v0.3 domain model currently contains Wallet, Category, Transaction, Money,
and Currency.

None of these implemented types requires a reusable date interval.

Introducing a generic `DateRange` now would require making decisions without a
real consumer.

The unresolved semantics include:

- whether the range represents calendar dates or exact moments;
- whether it should use `DateOnly` or `DateTimeOffset`;
- whether the start and end boundaries are inclusive;
- whether an open-ended range is allowed;
- whether equal start and end values represent a valid one-day range;
- whether budgets, reports, goals, and subscriptions share the same rules;
- whether operations such as `Contains`, `Overlaps`, and duration are required.

## Decision

Do not add a generic `DateRange` value object in v0.3.

Reconsider the type when the first implemented domain use case requires a date
interval.

The future implementation must be driven by that use case and must explicitly
define:

- the date and time type;
- boundary inclusion rules;
- whether both boundaries are required;
- allowed range length;
- equality semantics;
- required domain operations;
- domain error codes;
- unit tests.

## Rationale

A value object should represent a known domain concept and protect known
invariants.

At the current stage, only the structural idea of a range is known. Its actual
business meaning is not.

Deferring the type avoids:

- speculative abstractions;
- an overly generic contract;
- incorrect boundary semantics;
- unnecessary migration work;
- forcing different domain concepts to share one unsuitable type.

Adding the object later is inexpensive because no current entity or use case
depends on it.

## Consequences

Positive consequences:

- the v0.3 domain model remains focused on current business requirements;
- no unused production abstraction is introduced;
- future semantics can be defined from a real feature;
- budgets, reports, goals, and subscriptions remain free to use different
  period types if their rules differ.

Trade-offs:

- a future feature requiring a range must implement the value object or a more
  specific period type;
- transaction filtering may initially use application-level filter parameters
  until domain range behavior is required.

## Reconsideration trigger

Revisit this decision when implementing the first feature that requires a
period, such as:

- monthly budgets;
- reporting;
- financial goals;
- recurring subscriptions;
- reusable transaction date filtering.

At that point, first decide whether the correct concept is:

- a generic `DateRange`;
- `BudgetPeriod`;
- `ReportPeriod`;
- `GoalPeriod`;
- `SubscriptionPeriod`;
- another feature-specific value object.

## Alternatives considered

### Add DateRange in v0.3

Rejected because the type would have no current consumer and its semantics would
be speculative.

### Use two primitive date parameters everywhere

Acceptable temporarily at application boundaries, but not as a permanent
replacement when a real domain range with invariants appears.

### Create separate period types immediately

Rejected because those features and their invariants are not implemented yet.
