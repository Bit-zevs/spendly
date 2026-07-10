# Transactions

This folder contains the transaction-related domain model.

## Current domain types

- `TransactionId` — represents a strongly typed transaction identifier;
- `TransactionType` — defines the supported kinds of money movement.

## Supported transaction types

- `Income` — money received from outside the user's tracked wallets;
- `Expense` — money spent outside the user's tracked wallets;
- `Transfer` — money moved between the user's tracked wallets.

A transfer is neither income nor expense. It changes the balances of the
participating wallets but does not change the user's total amount of tracked
money.

The numeric value `0` is intentionally not assigned to a transaction type.
A future transaction entity must reject default and otherwise undefined
`TransactionType` values.

## Transaction identifiers

`TransactionId` provides a strongly typed identifier backed by `Guid`.

New identifiers are generated as version 7 UUIDs. Existing identifiers can be
restored through `TransactionId.From`, while an empty identifier is rejected.

## Not included yet

The current model intentionally does not contain:

- a transaction entity or aggregate;
- transaction creation rules;
- wallet balance changes;
- category assignment;
- transfer source and destination rules;
- refunds;
- cashback;
- corrections;
- split transactions;
- persistence mappings;
- Entity Framework Core attributes;
- API contracts or endpoints.

Additional transaction kinds and business rules should be introduced only when
their real use cases and invariants are known.

This folder must contain business rules only, not API endpoints, database
queries, or infrastructure integrations.
