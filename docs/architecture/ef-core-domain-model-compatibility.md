# EF Core Domain Model Compatibility

## Purpose

This document records the EF Core compatibility spike for the current Spendly
domain model.

The spike verifies that Entity Framework Core can persist and materialize the
immutable domain objects without adding public setters, public persistence
constructors, navigation properties required only by EF Core, or persistence
attributes to `Spendly.Domain`.

Production persistence remains outside the scope of this spike. The temporary
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

The current domain model is compatible with EF Core and PostgreSQL.

The model does not require:

- public setters;
- public or protected parameterless constructors on entities;
- EF Core attributes in the Domain project;
- persistence calls to public domain factories;
- navigation properties solely for defining foreign keys.

The compatibility test saves a wallet, income and expense categories, and
income and expense transactions. It disposes the write context, opens a new
context, reads all objects with `AsNoTracking()`, and compares the materialized
state with the original domain state.

## Mapping decisions demonstrated by the spike

| Domain element | Compatibility mapping |
| --- | --- |
| `WalletId`, `CategoryId`, `TransactionId` | Explicit `ValueConverter<TId, Guid>` and `ValueGeneratedNever()` |
| `Currency` | String code converter using `Currency.From` during materialization |
| `Money` | Required EF Core complex property with explicit backing-field access |
| `Money.Amount` | `decimal(19, 4)` through `HasPrecision(Money.Precision, Money.Scale)` |
| wallet and category names | Maximum lengths taken from `Wallet.MaxNameLength` and `Category.MaxNameLength` |
| timestamps | PostgreSQL `timestamp with time zone` with UTC domain values |
| relationships | Required foreign keys with `DeleteBehavior.Restrict`, without domain navigation properties |

## Domain materialization paths

`Wallet` and `Category` are materialized through their existing private
parameterized constructors.

`Transaction` uses a private scalar persistence constructor. The `Money`
complex property is restored separately through explicit field mapping.

`Money` keeps a private parameterless constructor exclusively for persistence
materialization. Application code must still use `Money.From`,
`Money.Positive`, or `Money.Zero`, so normal domain creation cannot bypass its
invariants.

## Money policy

The Domain and EF compatibility mapping now share one policy:

```text
precision: 19
scale: 4
maximum: 999999999999999.9999
```

The Domain rejects values that exceed the maximum or contain more than four
fractional digits. This prevents PostgreSQL from silently rounding or rejecting
a value that the Domain previously accepted.

## Transaction currency invariant

`Transaction.Create` receives the complete `Wallet`, verifies that
`Money.Currency` equals `Wallet.Currency`, and then stores only `WalletId`.

The compatibility mapping persists the amount currency as part of `Money`.
Production persistence should additionally consider whether the same rule needs
a database-level enforcement mechanism, because a normal PostgreSQL check
constraint cannot compare values across two tables.

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

## Production persistence requirements

The next persistence stage should:

1. create the real `SpendlyDbContext` in `Spendly.Infrastructure`;
2. move the final mappings out of the test project;
3. delete the temporary compatibility context and mappings after the production
   context is covered by equivalent tests;
4. add migrations instead of using `EnsureCreated()`;
5. add indexes and database constraints for supported domain invariants;
6. validate migrations against PostgreSQL Testcontainers;
7. add connection-string validation and a PostgreSQL readiness health check;
8. keep all EF Core-specific code outside `Spendly.Domain`.

## Out of scope

This spike does not add or approve:

- a production `SpendlyDbContext`;
- production entity configurations;
- migrations;
- repositories;
- application persistence interfaces;
- API connection strings;
- database health checks;
- transactional application use cases.
