# Application unit tests

This folder is reserved for unit tests that verify application-layer behavior.

Use this folder for tests related to:

- commands;
- queries;
- handlers;
- validators;
- application services;
- use cases;
- orchestration between domain objects and abstractions.

Application unit tests may use test doubles for dependencies such as repositories, clocks, current user providers, or external service abstractions.

Do not start the API, database, message broker, or any infrastructure service from application unit tests.
