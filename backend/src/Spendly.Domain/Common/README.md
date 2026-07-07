# Common

This folder contains shared domain building blocks used across multiple Spendly domain areas.

## Current contents

- `Entity<TId>` — base type for domain entities with stable identity.
- `ValueObject` — base type for immutable value-based domain objects.
- `IStronglyTypedId<TValue>` — contract for type-safe identifiers.

## Rules

The `Common` folder should contain only small, reusable domain concepts.

Allowed examples:

- base entity abstractions;
- base value object abstractions;
- strongly typed identifier contracts;
- shared domain primitives that are independent from application and infrastructure layers.

Do not place here:

- application services;
- infrastructure services;
- API contracts;
- database mappings;
- Entity Framework Core-specific code;
- HTTP-specific code.

The domain layer must remain pure C# code.
