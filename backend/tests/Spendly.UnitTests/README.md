# Spendly.UnitTests

This project contains isolated unit tests for the Spendly backend.

Unit tests should focus on business rules and application logic without starting the API, using a database, calling external services, or depending on infrastructure details.

## Test areas

- `Domain` — tests for entities, value objects, domain services, and domain rules.
- `Application` — tests for application use cases, commands, queries, handlers, validators, and application services.
- `TestUtilities` — shared helpers for unit tests, such as builders, factories, fake clocks, and custom assertions.

## Guidelines

- Keep unit tests fast and deterministic.
- Do not test ASP.NET Core endpoints in this project.
- Do not use real databases, HTTP clients, queues, or external services.
- Put API and infrastructure behavior tests into `Spendly.IntegrationTests`.
- Add test helpers only when they are needed by real tests.
