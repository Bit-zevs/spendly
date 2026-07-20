# EF Core Domain Model Compatibility

## Purpose

This document records the EF Core compatibility spike for the current Spendly
Domain model.

The spike verifies that Entity Framework Core can persist and materialize the
immutable Domain objects without adding public setters, public persistence
constructors, navigation properties required only by EF Core, or persistence
attributes to `Spendly.Domain`.

The accepted storage contract is defined by
[ADR 0003: Define domain model persistence strategy](../adr/0003-define-domain-model-persistence-strategy.md).
This document describes the evidence behind that decision and the temporary
test implementation that protects it.

Production persistence remains outside the scope of the spike. The temporary
`DbContext` and mappings live only in `Spendly.IntegrationTests`.

## Tested setup

The repository currently verifies the model with:

- .NET SDK `10.0.301`;
- Entity Framework Core `10.0.9`;
- Npgsql Entity Framework Core provider `10.0.3`;
- Testcontainers for PostgreSQL `4.13.0`;
- PostgreSQL image `postgres:17.10`;
- xUnit v3.

## Result

The current Domain model is compatible with EF Core and PostgreSQL.

The model does not require:

- public setters;
- public or protected parameterless constructors on entities;
- EF Core attributes in the Domain project;
- persistence calls to public Domain factories;
- navigation properties solely for defining foreign keys.

The real PostgreSQL round-trip test saves a wallet, income and expense
categories, and income and expense transactions. It disposes the write context,
opens a new context, reads all objects with `AsNoTracking()`, and compares the
materialized state with the original Domain state.

## Approved mapping demonstrated by the spike

| Domain element | PostgreSQL and EF Core mapping |
| --- | --- |
| `WalletId`, `CategoryId`, `TransactionId` | Explicit `ValueConverter<TId, Guid>`, PostgreSQL `uuid`, and `ValueGeneratedNever()` |
| `Currency` | Explicit `Currency` to `string` converter and `character varying(3)` |
| `Money` | Required EF Core complex property flattened into the containing table |
| `Money.Amount` | `numeric(19,4)` using `Money.Precision` and `Money.Scale` |
| `WalletType`, `CategoryType`, `TransactionType` | Explicit enum-to-`short` converter and PostgreSQL `smallint` |
| required timestamps | PostgreSQL `timestamp with time zone` with UTC Domain values |
| `Transaction.UpdatedAt` | Nullable PostgreSQL `timestamp with time zone` |
| relationships | Required foreign keys with `DeleteBehavior.Restrict` and explicit constraint names |
| transaction indexes | Explicit indexes for `wallet_id`, `category_id`, and `occurred_at` |
| database checks | Currency format, valid enum codes, and positive transaction amount |

The test mappings intentionally use explicit physical names and types. They are
an executable specification for the future Infrastructure mappings, not a
production persistence layer.

## Value converters and comparers

The compatibility mapping uses converters for:

- every strongly typed identifier;
- `Currency`;
- persisted enums converted to `short`.

No custom value comparers are configured.

The strongly typed identifiers are immutable `readonly record struct` values.
`Currency` is immutable and implements value equality. Their normal equality
and snapshot behavior is sufficient for the current model. A comparer should be
introduced only when a future mapped type needs custom equality, hashing, or a
deep snapshot.

## Domain materialization paths

`Wallet` and `Category` are materialized through their existing private
parameterized constructors.

`Transaction` uses a private scalar persistence constructor. The `Money`
complex property is restored separately through explicit backing-field mapping.

`Money` keeps a private parameterless constructor exclusively for persistence
materialization. Application code must still use `Money.From`,
`Money.Positive`, or `Money.Zero`, so normal Domain creation cannot bypass its
invariants.

## Money policy

The Domain and compatibility mapping share one policy:

```text
precision: 19
scale: 4
maximum: 999999999999999.9999
```

The Domain rejects values that exceed the maximum or contain more than four
fractional digits. The database stores the exact decimal value as
`numeric(19,4)` and never passes through `float` or `double`.

A transaction amount also has the database check `amount > 0`, matching the
current `Transaction.Create` contract.

## Transaction currency invariant

`Transaction.Create` receives the complete `Wallet`, verifies that
`Money.Currency` equals `Wallet.Currency`, and then stores only `WalletId`.

The compatibility mapping persists the transaction currency as part of
`Money`. The accepted strategy keeps the cross-table currency rule in Domain and
Application logic. A normal PostgreSQL row check cannot compare the transaction
currency with the referenced wallet currency.

Introducing a trigger or a denormalized compound foreign key for this rule
requires a separate decision backed by a real use case.

## Naming and integrity checks

The temporary model demonstrates the accepted lowercase `snake_case` strategy:

```text
wallets
categories
transactions
currency_code
created_at
occurred_at
updated_at
```

It also verifies explicit names such as:

```text
pk_transactions
fk_transactions_wallets_wallet_id
ck_transactions_amount_positive
ix_transactions_wallet_id
```

The following checks are represented in the compatibility model:

- wallet currency codes contain exactly three uppercase ASCII letters;
- wallet types contain only defined numeric codes;
- category types contain only defined numeric codes;
- transaction types contain only defined numeric codes;
- transaction amounts are positive;
- transaction currency codes contain exactly three uppercase ASCII letters.

## Explicit Docker test

The real PostgreSQL round-trip is marked as an explicit xUnit v3 test. Normal
local integration-test execution does not require Docker.

Run fast integration tests:

```bash
dotnet test tests/Spendly.IntegrationTests/Spendly.IntegrationTests.csproj
```

Run all integration tests, including Testcontainers:

```bash
dotnet test tests/Spendly.IntegrationTests/Spendly.IntegrationTests.csproj \
  --settings tests/docker.runsettings
```

CI uses the second command.

The model-strategy tests do not open a database connection. They inspect the
finalized Npgsql EF Core model and protect column types, converters, constraints,
index names, and delete behavior during normal test execution.

## Production persistence requirements

The next persistence stage should:

1. create the real `SpendlyDbContext` in `Spendly.Infrastructure`;
2. move equivalent final mappings out of the test project;
3. add the first migration;
4. validate migrations against PostgreSQL Testcontainers with `MigrateAsync()`;
5. add connection-string validation and a PostgreSQL readiness health check;
6. create persistence ports only with real Application use cases;
7. keep all EF Core-specific code outside `Spendly.Domain`;
8. remove the temporary compatibility context after production mappings have
   equivalent model and round-trip coverage.

The production API and Worker must not apply migrations automatically during
startup. Migrations belong to an explicit deployment step, as defined by ADR
0003.

## Out of scope

This spike does not add:

- a production `SpendlyDbContext`;
- production entity configurations;
- migrations;
- repositories;
- application persistence interfaces;
- API connection strings;
- database health checks;
- transactional application use cases.
