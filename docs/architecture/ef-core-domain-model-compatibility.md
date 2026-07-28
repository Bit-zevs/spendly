# EF Core Domain Model Compatibility

## Purpose

This document records the compatibility work that proved the immutable Spendly
Domain model could be persisted by EF Core and Npgsql without adding
persistence concerns to `Spendly.Domain`.

The original spike has been completed and promoted into the production
persistence layer. Current implementation and operational instructions are
documented in [Persistence Architecture](persistence.md).

## Outcome

The compatibility result is successful.

EF Core and PostgreSQL can persist and materialize the current Domain model
without:

- public setters;
- public entity constructors for persistence;
- EF Core attributes in Domain;
- navigation properties added only for mapping;
- calling public Domain factories during materialization;
- custom value comparers for the current immutable converted values.

## Production promotion

The validated mapping now lives in:

```text
backend/src/Spendly.Infrastructure/Persistence/
```

Production components include:

- `SpendlyDbContext`;
- `WalletConfiguration`;
- `CategoryConfiguration`;
- `TransactionConfiguration`;
- reusable `Currency` and `Money` mapping extensions;
- converters for strongly typed identifiers, currencies, and enums;
- the `InitialCreate` migration;
- PostgreSQL database integration tests.

The temporary test-only compatibility context described by the original spike
no longer represents the repository architecture. Tests now exercise the
production context and mappings directly.

## Verified setup

The repository pins the tested toolchain in source-controlled configuration:

- .NET SDK `10.0.301` in `global.json`;
- EF Core `10.0.10` in `backend/Directory.Packages.props`;
- Npgsql EF Core provider `10.0.3`;
- Testcontainers for PostgreSQL `4.13.0`;
- PostgreSQL image `postgres:17.10`;
- xUnit v3.

These values describe the current repository state. Package and image upgrades
must update code, tests, and documentation together.

## Approved mapping

| Domain element | PostgreSQL and EF Core mapping |
| --- | --- |
| `WalletId`, `CategoryId`, `TransactionId` | Explicit `ValueConverter<TId, Guid>`, PostgreSQL `uuid`, and `ValueGeneratedNever()` |
| `Currency` | Explicit `Currency` to `string` converter and `character varying(3)` |
| `Money` | Required EF Core complex property flattened into the containing table |
| `Money.Amount` | `numeric(19,4)` using `Money.Precision` and `Money.Scale` |
| `WalletType`, `CategoryType`, `TransactionType` | Explicit enum-to-`short` converter and PostgreSQL `smallint` |
| required timestamps | PostgreSQL `timestamp with time zone` with UTC Domain values |
| `Transaction.UpdatedAt` | Nullable PostgreSQL `timestamp with time zone` |
| relationships | Required foreign keys with `DeleteBehavior.Restrict` and explicit names |
| transaction indexes | Explicit indexes for `wallet_id`, `category_id`, and `occurred_at` |
| database checks | Currency format, valid enum codes, and positive transaction amount |

The accepted contract is defined by
[ADR 0003](../adr/0003-define-domain-model-persistence-strategy.md).

## Materialization paths

`Wallet` and `Category` are materialized through private constructors selected
by EF Core.

`Transaction` uses private persistence state and backing-field access. Its
`Money` value is mapped as a required complex property.

`Money` keeps a private parameterless constructor for persistence
materialization. Normal application code must continue to use `Money.From`,
`Money.Positive`, or `Money.Zero`, so Domain creation rules remain enforced.

## Converter and comparer result

Converters are required for:

- every strongly typed identifier;
- `Currency`;
- persisted enums converted to `short`.

Custom comparers are not required for the current mappings. Strongly typed IDs
are immutable record structs, and `Currency` is immutable with value equality.
EF Core can use their normal equality and snapshot behavior.

A custom comparer should be introduced only when a future mapped type requires
custom equality, hashing, or deep snapshots.

## Money result

Domain and database share:

```text
precision: 19
scale: 4
maximum: 999999999999999.9999
```

PostgreSQL stores exact values as `numeric(19,4)`. The persistence path remains
`decimal` and does not pass through `float` or `double`.

The `transactions` table also enforces `amount > 0`.

## Cross-table currency rule

`Transaction.Create` verifies that the transaction amount currency equals the
wallet currency and then stores only the wallet identifier.

The database stores the transaction currency as part of `Money`, but a normal
row check cannot compare it with a referenced wallet row. The rule therefore
remains a Domain and Application responsibility.

A trigger or denormalized compound foreign key would require a separate ADR and
a demonstrated need.

## Naming and integrity result

The production model uses explicit lowercase `snake_case` names, including:

```text
wallets
categories
transactions
currency_code
created_at
occurred_at
updated_at
```

Representative database object names include:

```text
pk_transactions
fk_transactions_wallets_wallet_id
ck_transactions_amount_positive
ix_transactions_wallet_id
```

The model verifies:

- exactly three uppercase ASCII letters for currency codes;
- only defined wallet, category, and transaction numeric codes;
- positive transaction amounts;
- restrictive transaction foreign keys;
- explicit physical column types and names.

## Current test evidence

Metadata tests inspect the finalized production Npgsql model without opening a
database connection.

Explicit Testcontainers tests use the production context and migrations to
verify:

- migration application to an empty PostgreSQL database;
- physical schema shape;
- write/read materialization through separate contexts;
- strongly typed IDs and value objects;
- foreign-key restrictions;
- migration rollback and reapplication;
- queryability of every production `DbSet`;
- readiness without schema mutation.

Run the default integration tests:

```bash
cd backend
dotnet test tests/Spendly.IntegrationTests/Spendly.IntegrationTests.csproj
```

Run explicit PostgreSQL tests:

```bash
dotnet test tests/Spendly.IntegrationTests/Spendly.IntegrationTests.csproj --settings tests/docker.runsettings
```

## Remaining boundaries

Compatibility and production persistence do not add:

- Application use cases;
- repositories or Application persistence ports;
- wallet, category, or transaction HTTP endpoints;
- automatic startup migrations;
- cross-table database enforcement of transaction and wallet currency equality.

Those concerns require separate use-case or architecture decisions.
