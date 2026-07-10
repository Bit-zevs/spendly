# Wallets

This folder contains the wallet-related domain model.

## Current domain types

- `Wallet` — represents a source or container through which money is stored or managed;
- `WalletId` — represents a strongly typed wallet identifier;
- `WalletType` — defines the supported kinds of wallets.

## Wallet properties

A wallet currently contains:

- a stable `WalletId`;
- a non-empty user-visible name;
- a supported `WalletType`;
- a required `Currency`;
- a UTC creation timestamp.

## Wallet invariants

A valid wallet must satisfy the following rules:

- its identifier must not be empty;
- its name must not be null, empty, or whitespace;
- its name is trimmed before it is stored;
- its type must be declared in `WalletType`;
- its currency must be provided;
- its creation timestamp must not have the default value;
- its creation timestamp is stored in UTC.

Wallet instances are created through `Wallet.Create`. The constructor is private so that callers cannot bypass domain validation.

## Supported wallet types

- `Cash` — physical cash;
- `DebitCard` — a debit card backed by the owner's own funds;
- `CreditCard` — a credit card backed by borrowed funds;
- `BankAccount` — a general-purpose bank account;
- `Savings` — an account primarily intended for saving money;
- `Investment` — an investment account or portfolio;
- `Other` — another supported kind of wallet that does not fit the known categories.

The numeric value `0` is intentionally not assigned to a wallet type. The wallet entity rejects default and otherwise undefined `WalletType` values.

## Not included yet

The current model intentionally does not contain:

- balance;
- transactions;
- user ownership;
- persistence mappings;
- Entity Framework Core attributes;
- API contracts;
- CRUD operations.

Balance rules and transaction history will be designed separately when the corresponding business requirements are introduced.

Persistence details, database mappings, and API contracts must not be placed in the domain model.
