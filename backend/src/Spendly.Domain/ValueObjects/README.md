# ValueObjects

This folder is reserved for domain value objects.

A value object is identified by its value rather than by a separate identity.

Possible future examples:

- `Money`;
- `Currency`;
- `DateRange`;
- `Month`;
- `Percentage`.

Value objects should usually be immutable and should protect their own invariants.

For example, `Money` should not allow an invalid amount or an unsupported currency.
