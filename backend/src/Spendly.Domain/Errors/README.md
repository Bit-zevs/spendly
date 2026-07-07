# Errors

This folder is reserved for domain-level errors.

Domain errors describe business rule violations, not HTTP or infrastructure failures.

Possible future examples:

- wallet balance is insufficient;
- transaction amount is invalid;
- category name is empty;
- currency is not supported;
- budget limit is exceeded.

Domain errors should not know anything about HTTP status codes, controllers, Problem Details, databases, or external services.
