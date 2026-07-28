# MVP Scope

This document defines the target product scope of the first useful Spendly MVP.
It is a scope contract, not a statement that every listed capability is already
implemented.

The first MVP is intended for one user who tracks personal finances manually.

## Included in the target MVP

- registration and login;
- wallet management;
- category management;
- income and expense transactions;
- transaction history;
- monthly budget;
- daily safe-spend calculation;
- basic dashboard.

## Outside the first MVP

- bank integrations;
- shared and family budgets;
- payments;
- premium subscriptions;
- mobile applications;
- advanced analytics.

## Current delivery status

The repository is currently at `v0.4 Persistence Layer`. The backend foundation,
Domain model, production PostgreSQL persistence, migrations, database readiness,
integration tests, and backend CI are implemented.

Authentication, Application use cases, repositories, domain feature endpoints,
budgets, safe-spend calculation, transaction history, and the dashboard are not
implemented yet.

See [Product Delivery Status](delivery-status.md) for the detailed implementation
matrix.

## Scope changes

A change to the first-MVP boundary should update this document and the product
delivery status in the same pull request. Implementation progress alone should
update the delivery status without silently changing the agreed MVP scope.
