# Errors

This folder contains domain-level error types.

Domain errors describe business rule violations inside the domain model. They do not describe HTTP, persistence, infrastructure, or UI failures.

## Main types

- `DomainError` — immutable value that contains a stable machine-readable `Code` and human-readable `Message`.
- `DomainException` — exception used by domain objects when a business invariant is violated.
- `DomainErrors` — catalog of known domain errors that can be reused by entities and value objects.

## Rules

Domain errors must not depend on API concepts such as HTTP status codes, `ProblemDetails`, `BadRequest`, `NotFound`, controllers, endpoints, or response models.

Examples:

- `Currency.Code.Required`
- `Currency.Code.InvalidFormat`
- `Money.Amount.Negative`
- `Money.Amount.NotPositive`
- `Money.Currency.Required`
- `Money.Currency.Mismatch`
- `Wallet.Name.Empty`
- `Category.Name.Empty`
- `Transaction.Wallet.Required`
- `Transaction.Amount.NotPositive`

In the API layer these errors may later be translated to HTTP responses, but the domain layer should only expose business meaning.
