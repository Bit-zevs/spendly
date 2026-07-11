# Transactions

This folder contains the transaction-related domain model.

A transaction represents a supported money movement and protects the
relationship between its type, amount, wallet, category, timestamps, and
optional description.

## Current domain types

- `Transaction` — represents an immutable record of a supported money movement;
- `TransactionId` — represents a strongly typed transaction identifier;
- `TransactionType` — defines the known kinds of money movement.

## Known transaction types

- `Income` — money received from outside the user's tracked wallets;
- `Expense` — money spent outside the user's tracked wallets;
- `Transfer` — money moved between the user's tracked wallets.

A transfer is neither income nor expense. It changes the balances of the
participating wallets but does not change the user's total amount of tracked
money.

## Current entity support

The current `Transaction` entity supports:

- income transactions;
- expense transactions.

Although `Transfer` is a known transaction type, transfer creation is
intentionally rejected by the current entity.

A correct transfer model requires at least a source wallet and a destination
wallet. Multi-currency transfers may additionally require separate source and
destination amounts, an exchange rate, and fees. Those rules must be introduced
by a dedicated future task.

## Transaction rules

Every currently supported transaction must:

- have a defined transaction type;
- have a positive amount;
- be associated with a wallet;
- have a category;
- use an income category for an income transaction;
- use an expense category for an expense transaction;
- have a non-default occurrence time;
- have a non-default creation time;
- have no description longer than `Transaction.MaxDescriptionLength`.

Transaction amounts are always positive. The transaction type defines the
direction of the money movement.

Descriptions are optional. Empty or whitespace-only descriptions are stored as
`null`, while meaningful descriptions are trimmed.

Occurrence and creation timestamps are stored in UTC.

## Category validation

The transaction factory receives a `Category` entity so that it can verify the
category type.

After validation, the transaction stores only the non-nullable category
identifier. This prevents callers from supplying a category identifier and an
unrelated category type as separate, contradictory values and reflects that all
currently supported transactions require a category.

## Transaction identifiers

`TransactionId` provides a strongly typed identifier backed by `Guid`.

New identifiers are generated as version 7 UUIDs. Existing identifiers can be
restored through `TransactionId.From`, which rejects `Guid.Empty`.

## Not included yet

The current model intentionally does not contain:

- wallet balance changes;
- transfer source and destination rules;
- transaction editing;
- transaction deletion;
- refunds;
- cashback;
- corrections;
- split transactions;
- recurring transaction rules;
- persistence mappings;
- Entity Framework Core attributes;
- API contracts or endpoints.

Additional transaction kinds and business rules should be introduced only when
their real use cases and invariants are known.

This folder must contain business rules only, not API endpoints, database
queries, or infrastructure integrations.
