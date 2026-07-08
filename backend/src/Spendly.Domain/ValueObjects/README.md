# ValueObjects

This folder contains domain value objects.

A value object is identified by its value rather than by a separate identity.

Current value objects:

- `Currency` — represents a normalized three-letter currency code.
- `Money` — represents a non-negative monetary amount in a specific currency.

Possible future examples:

- `DateRange`;
- `Month`;
- `Percentage`.

Value objects should usually be immutable and should protect their own invariants.

For example, `Currency` should not allow an empty code or an invalid currency code format.

`Money` should not allow an invalid amount or a missing currency. It should also prevent arithmetic and comparison operations between different currencies unless an explicit currency conversion mechanism exists.
