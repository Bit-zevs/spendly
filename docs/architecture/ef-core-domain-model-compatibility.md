# EF Core Domain Model Compatibility

## Purpose

This document records the result of the EF Core compatibility spike for the
current Spendly domain model.

The goal of the spike was to verify that Entity Framework Core can persist and
materialize the immutable domain objects without introducing public setters,
public persistence constructors, or EF Core attributes into
`Spendly.Domain`.

The spike covers:

- private and protected constructors;
- read-only public properties;
- `Entity<TId>`;
- strongly typed identifiers;
- `Currency`;
- `Money`;
- `DateTimeOffset`;
- nullable `Transaction.UpdatedAt`;
- materialization without calling public domain factories.

Production persistence is intentionally outside the scope of this document.
The compatibility `DbContext` and entity configurations remain in the
integration-test project only.

## Decision status

The compatibility task is complete for the current Domain model. The real
PostgreSQL round-trip demonstrates that the model can be materialized without
public setters, public persistence constructors, or EF Core references in the
Domain project.

This is a compatibility result, not approval of a production schema. Decimal
precision, database constraints, migrations, indexes, and operational
persistence behavior remain separate implementation decisions.

## Tested setup

The repository currently verifies the model with:

- .NET SDK `10.0.301`;
- Entity Framework Core `10.0.9`;
- Npgsql Entity Framework Core provider `10.0.2`;
- Testcontainers for PostgreSQL `4.13.0`;
- PostgreSQL image `postgres:17.10`;
- xUnit v3 integration tests.

The test creates real PostgreSQL tables, saves domain objects, disposes the
write context, opens a new context, reads the objects with `AsNoTracking()`,
and compares the materialized state with the original state.

## Result

The current domain model is compatible with EF Core and PostgreSQL after the
minimal, narrowly scoped materialization changes already present in `Money`
and `Transaction`.

The model does not require:

- public setters;
- public or protected parameterless constructors on entities;
- EF Core attributes in the Domain project;
- persistence calls to public domain factories;
- navigation properties solely for defining foreign keys.

Production EF Core configurations will still need explicit converters, field
mapping, complex-property mapping, column definitions, keys, and constraints.

## Compatibility summary

| Domain element | Materialization result | Required production mapping or change |
| --- | --- | --- |
| `Wallet` | Materialized through its private parameterized constructor | Explicit property mappings and converters for `WalletId` and `Currency` |
| `Category` | Materialized through its private parameterized constructor | Explicit property mappings and a converter for `CategoryId` |
| `Transaction` | Materialized through its private persistence constructor | Field mapping for `Amount` and `UpdatedAt`, converters for identifiers, and relationship configuration |
| `Entity<TId>` | Works through the derived entity constructors | No parameterless base constructor is required |
| strongly typed IDs | Round-trip successfully as PostgreSQL `uuid` values | `ValueConverter<TId, Guid>` and `ValueGeneratedNever()` |
| `Currency` | Round-trips successfully as a normalized string code | `ValueConverter<Currency, string>` using `Currency.From` when reading |
| `Money` | Round-trips successfully as an EF Core complex property | Private parameterless materialization constructor and field mapping for `_amount` and `_currency` |
| `DateTimeOffset` | Round-trips successfully as UTC timestamps | `timestamp with time zone` columns and UTC values |
| `Transaction.UpdatedAt` | Both non-null and null values materialize correctly | Nullable column mapped to `_updatedAt` with field access |

## Entity constructors

### Wallet

`Wallet` can be materialized through its existing private constructor:

```csharp
private Wallet(
    WalletId id,
    string name,
    WalletType type,
    Currency currency,
    DateTimeOffset createdAt)
    : base(id)
```

All constructor parameters correspond to mapped scalar properties after value
conversion. No special parameterless constructor is required. Constructor
binding is convention-based, so parameter names and CLR types are part of the
materialization contract and should be protected by regression tests.

### Category

`Category` can be materialized through its existing private constructor:

```csharp
private Category(
    CategoryId id,
    string name,
    CategoryType type,
    DateTimeOffset createdAt)
    : base(id)
```

No Domain change is required for `Category` materialization.

### Transaction

`Transaction` contains a `Money` complex property. The compatibility spike uses
a private persistence constructor containing only the scalar properties that
EF Core can bind directly:

```csharp
private Transaction(
    TransactionId id,
    TransactionType type,
    WalletId walletId,
    CategoryId categoryId,
    DateTimeOffset occurredAt,
    string? description,
    DateTimeOffset createdAt)
    : base(id)
```

`Money` is restored separately through the `_amount` backing field.

The normal domain constructor continues to accept a valid `Money` instance and
delegates the scalar initialization to the materialization constructor. Public
creation remains available only through `Transaction.Create`.

This is a minimal persistence accommodation. It does not expose an invalid
construction path to application code.

## Entity<TId>

`Entity<TId>` does not need a parameterless constructor.

Each concrete entity constructor receives its strongly typed identifier and
calls `base(id)`. This keeps the base-class invariant active during both normal
creation and EF Core materialization:

```csharp
protected Entity(TId id)
{
    if (EqualityComparer<TId>.Default.Equals(id, default!))
    {
        throw new ArgumentException(
            "Entity id cannot be the default value.",
            nameof(id));
    }

    Id = id;
}
```

A parameterless base constructor would allow a derived type to initialize an
entity with a default identifier and would weaken the identity invariant.
Therefore, it should not be added solely for EF Core.

## Read-only properties and field mapping

Read-only public properties can remain read-only.

Properties initialized through an entity constructor do not require public
setters. Properties that EF Core must populate after construction require
explicit backing-field configuration.

The current spike requires field mapping for:

- `Transaction.Amount` to `Transaction._amount`;
- `Transaction.UpdatedAt` to `Transaction._updatedAt`;
- `Money.Amount` to `Money._amount`;
- `Money.Currency` to `Money._currency`.

The intended access mode is `PropertyAccessMode.Field`, ensuring that EF Core
reads and writes the configured fields rather than requiring public mutation
APIs.

## Strongly typed identifiers

`WalletId`, `CategoryId`, and `TransactionId` wrap `Guid` values and should
remain distinct domain types.

Each identifier requires a converter between the domain type and PostgreSQL
`uuid`:

```csharp
new ValueConverter<WalletId, Guid>(
    id => id.Value,
    value => WalletId.From(value));
```

Equivalent converters are required for `CategoryId` and `TransactionId`.

The reverse conversion must use each identifier's `From` factory so invalid
stored values, such as `Guid.Empty`, are rejected instead of silently entering
the domain model.

Identifiers are created by the Domain before persistence, so their properties
must be configured with:

```csharp
.ValueGeneratedNever();
```

This prevents EF Core from treating the database as the identifier generator.

The identifiers are immutable record structs and already provide value
equality, so the current model does not require custom `ValueComparer`
instances. This decision should be revisited only if an identifier becomes a
mutable reference type or contains a mutable value.

## Currency

`Currency` should be stored as its three-letter normalized code.

The required converter is:

```csharp
new ValueConverter<Currency, string>(
    currency => currency.Code,
    code => Currency.From(code));
```

Using `Currency.From` during materialization preserves normalization and format
validation. The database column should be required and limited to
`Currency.CodeLength` characters.

`Wallet.Currency` and `Money.Currency` can use the same converter.

## Money

`Money` is mapped as an EF Core complex property rather than as a separate
entity.

It is stored in the containing transaction row using two columns:

- `amount` for the decimal value;
- `currency_code` for the currency code.

`Money` requires its existing private parameterless constructor so EF Core can
create the complex object before populating its fields:

```csharp
private Money()
{
}
```

This constructor is inaccessible to application code. Normal creation still
uses:

- `Money.From`;
- `Money.Positive`;
- `Money.Zero`.

The complex-property configuration must map:

- `Transaction.Amount` to `Transaction._amount`;
- `Money.Amount` to `Money._amount`;
- `Money.Currency` to `Money._currency`.

The currency field also requires the `Currency` value converter.

The compatibility mapping deliberately uses unconstrained PostgreSQL
`numeric`, because the Domain has not yet defined a monetary precision and
scale policy. Production persistence must not copy this choice blindly. A
separate decision must define supported amount range, fractional digits, and
rounding before configuring `HasPrecision(...)` and generating migrations.

## DateTimeOffset

All current entity timestamps are normalized to UTC by the Domain.

The PostgreSQL columns should use:

```text
timestamp with time zone
```

The compatibility test verifies that values supplied with non-zero offsets are
stored and materialized with `TimeSpan.Zero` after the Domain normalizes them
through `ToUniversalTime()`.

This applies to:

- `Wallet.CreatedAt`;
- `Category.CreatedAt`;
- `Transaction.OccurredAt`;
- `Transaction.CreatedAt`;
- `Transaction.UpdatedAt`.

Production code must continue passing UTC `DateTimeOffset` values to Npgsql.
The database type represents an instant in time and does not preserve the
original input offset.

PostgreSQL timestamps have microsecond precision, while .NET timestamps can
represent 100-nanosecond ticks. Values containing sub-microsecond ticks may
therefore be rounded or truncated during persistence. Production code must
either normalize timestamps to microsecond precision before comparison or
avoid assuming exact round-trip equality for finer-grained ticks.

## Nullable Transaction.UpdatedAt

`Transaction.UpdatedAt` is a read-only nullable property backed by
`_updatedAt`.

It should be configured with explicit field access:

```csharp
builder
    .Property(transaction => transaction.UpdatedAt)
    .HasField("_updatedAt")
    .UsePropertyAccessMode(PropertyAccessMode.Field)
    .HasColumnName("updated_at")
    .HasColumnType("timestamp with time zone");
```

No `IsRequired()` call should be applied. The compatibility test verifies both:

- a transaction with a non-null `UpdatedAt` value;
- a transaction whose `UpdatedAt` remains `null`.

## Relationships without navigation properties

The current domain model stores `WalletId` and `CategoryId` in `Transaction`
but does not expose persistence navigation properties.

EF Core can still define required relationships using foreign-key properties:

```csharp
builder
    .HasOne<Wallet>()
    .WithMany()
    .HasForeignKey(transaction => transaction.WalletId);
```

The same pattern applies to `Category`.

Production mappings should use restrictive delete behavior so deleting a
wallet or category cannot silently remove financial history.

## Materialization and domain factories

EF Core does not call these public factories while reading rows:

- `Wallet.Create`;
- `Category.Create`;
- `Transaction.Create`;
- `Money.From` or `Money.Positive` for the complete `Money` object.

Constructor binding, value converters, and field mapping restore the persisted
state directly.

This means successful materialization proves technical compatibility, but it
does not make the database incapable of containing invalid data. Domain
factories protect new state created through application code; database
constraints must protect persisted state from invalid imports, manual SQL, or
future implementation defects.

## Required production database constraints

When production persistence is introduced, its migrations should mirror the
important domain invariants where practical.

At minimum, consider:

- primary keys for every entity;
- non-null identifiers and required properties;
- foreign keys from transactions to wallets and categories;
- restrictive delete behavior for financial history;
- a positive transaction amount check;
- maximum transaction description length of 500 characters;
- three-character currency codes;
- allowed enum values for wallet, category, and transaction types;
- UTC-compatible timestamp column types;
- indexes required by transaction queries.

Database constraints complement the Domain. They do not replace domain
factories or business behavior.

The spike uses `EnsureCreated()` only to create an isolated disposable schema.
It does not validate migration generation, migration ordering, or upgrade paths.
Production persistence must be verified through real migrations against a
fresh database and against at least one supported previous schema version.

## Decisions for future production configurations

Production entity configurations should:

1. stay in `Spendly.Infrastructure`, not `Spendly.Domain`;
2. use separate `IEntityTypeConfiguration<T>` classes;
3. reuse centralized value converters for strongly typed IDs and `Currency`;
4. mark Domain-generated IDs with `ValueGeneratedNever()`;
5. map `Money` as a required complex property;
6. use explicit field mapping for `Money` and `Transaction.UpdatedAt`;
7. use PostgreSQL `uuid`, `numeric`, and `timestamp with time zone` types;
8. configure required foreign keys without forcing navigation properties into
   the Domain;
9. reproduce important domain invariants as database constraints;
10. define explicit decimal precision and scale before mapping `Money`;
11. account for PostgreSQL microsecond timestamp precision;
12. validate generated migrations with PostgreSQL Testcontainers;
13. keep all EF Core-specific code outside the Domain project.

## Minimal Domain changes

The compatibility spike requires only these narrowly scoped Domain changes:

- keep the private parameterless constructor in `Money` for complex-property
  materialization;
- keep the private scalar materialization constructor in `Transaction`;
- keep the `_amount` and `_updatedAt` backing fields used by explicit mapping;
- do not add a parameterless constructor to `Entity<TId>`;
- do not add public setters or EF Core attributes.

No additional Domain changes are currently required for `Wallet`, `Category`,
strongly typed IDs, `Currency`, or timestamp properties.

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

Those items belong to the next persistence implementation stage and should use
the decisions recorded here.

## Verification

Run all backend tests from the `backend` directory:

```powershell
dotnet test Spendly.sln
```

Run only the compatibility test with Docker available:

```powershell
dotnet test tests/Spendly.IntegrationTests/Spendly.IntegrationTests.csproj `
  --filter "FullyQualifiedName~EfCoreDomainModelCompatibilityTests"
```

A successful run confirms that the configured model can round-trip through a
real PostgreSQL instance.

## Conclusion

The immutable Spendly domain model can be used with EF Core without exposing
public mutation APIs or adding persistence attributes to the Domain project.

`Wallet` and `Category` work through their existing private constructors.
`Transaction` and `Money` require the private materialization paths already
present in the model. Strongly typed IDs and `Currency` require value
converters, while `Money` and `UpdatedAt` require explicit field mapping.

The protected parameterless constructor previously added to `Entity<TId>` is
not required and should remain absent to preserve the non-default identifier
invariant. `Entity<TId>.Id` can remain a getter-only property because all
concrete entities use constructor binding.
