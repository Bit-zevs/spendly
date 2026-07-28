# Product Delivery Status

This document tracks what is actually implemented in the repository. Product
vision and MVP scope describe the intended product; they must not be read as a
list of already delivered features.

## Current milestone

```text
v0.4 Persistence Layer
```

The current milestone provides the backend and persistence foundation required
for future product features. It does not yet provide end-user finance workflows.

## Implemented foundation

| Area | Status | Current implementation |
| --- | --- | --- |
| Backend solution | Implemented | .NET 10 solution with API, Application, Domain, Infrastructure, Worker, unit-test, and integration-test projects |
| API foundation | Implemented | validated configuration, Serilog, ProblemDetails, root status, liveness, readiness, OpenAPI, and Scalar |
| Domain foundations | Implemented | entities, value objects, strongly typed UUID version 7 identifiers, domain errors, and invariant protection |
| Wallet domain model | Implemented | immutable `Wallet`, `WalletId`, `WalletType`, currency, name, and creation-time rules |
| Category domain model | Implemented | immutable `Category`, `CategoryId`, `CategoryType`, name, type, and creation-time rules |
| Transaction domain model | Implemented for income and expense | immutable `Transaction`, positive `Money`, wallet-currency validation, category-direction validation, timestamps, and description rules |
| PostgreSQL persistence | Implemented | production `SpendlyDbContext`, explicit mappings, converters, constraints, indexes, and restrictive foreign keys |
| Database migrations | Implemented | committed `InitialCreate` migration and model snapshot |
| Database readiness | Implemented | PostgreSQL connectivity check through the shared `NpgsqlDataSource` |
| Automated verification | Implemented | unit tests, API tests, EF Core metadata tests, PostgreSQL round-trip tests, migration tests, readiness tests, and backend CI |

## MVP feature delivery

| MVP capability | Delivery status |
| --- | --- |
| Registration and login | Not implemented |
| Wallet management use cases and HTTP API | Domain and persistence foundation implemented; Application use cases and endpoints not implemented |
| Category management use cases and HTTP API | Domain and persistence foundation implemented; Application use cases and endpoints not implemented |
| Income and expense transaction use cases and HTTP API | Domain and persistence foundation implemented; Application use cases and endpoints not implemented |
| Transaction history | Not implemented |
| Monthly budget | Not implemented |
| Daily safe-spend calculation | Not implemented |
| Basic dashboard | Not implemented |

## Current architectural boundary

The repository currently has no production Application commands, queries,
handlers, persistence ports, repository implementations, authentication flow,
or wallet, category, transaction, budget, and reporting endpoints.

The next feature milestone should introduce a complete vertical product slice:

```text
HTTP contract
    -> Application use case
    -> Domain behavior
    -> focused Application persistence port
    -> Infrastructure implementation
    -> automated tests and updated documentation
```

Generic CRUD repositories and speculative abstractions remain intentionally
absent. Persistence contracts should be introduced only by concrete use cases.

## Post-MVP status

Bank integrations, shared or family budgets, payments, premium subscriptions,
mobile applications, advanced analytics, subscription management, financial
goals, and Telegram input are not implemented and remain outside the first MVP
unless the product scope is explicitly revised.
