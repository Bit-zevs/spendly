# Wallets

This folder contains the wallet-related domain model.

Current domain types:

- `WalletId` — represents a strongly typed wallet identifier;
- `WalletType` — defines the supported kinds of wallets.

Supported wallet types:

- `Cash` — physical cash;
- `DebitCard` — a debit card backed by the owner's own funds;
- `CreditCard` — a credit card backed by borrowed funds;
- `BankAccount` — a general-purpose bank account;
- `Savings` — an account primarily intended for saving money;
- `Investment` — an investment account or portfolio;
- `Other` — another supported kind of wallet that does not fit the known categories.

The numeric value `0` is intentionally not assigned to a wallet type. A future wallet aggregate must reject default or otherwise undefined `WalletType` values.

Possible future contents:

- wallet aggregate;
- wallet name value object;
- wallet balance rules;
- wallet-related domain errors.

A wallet represents a source or container of money, such as cash, a card, a bank account, or another financial account.

Persistence details, database mappings, and API contracts should not be placed here.
