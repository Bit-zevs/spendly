# Spendly.IntegrationTests

Integration tests for Spendly.Api.

The tests start the API in memory through `WebApplicationFactory<Program>` and send HTTP requests through `HttpClient`.

Current scope:

- API host smoke tests
- health check endpoint tests
- ProblemDetails response tests
- OpenAPI / Scalar availability tests

Current limitations:

- no PostgreSQL
- no Docker
- no EF Core database tests
- no Testcontainers

Database-backed integration tests will be added later when persistence is introduced.
