# Categories

This folder contains the category-related domain model.

## Current domain types

- `Category` — represents a classification assigned to income or expense transactions;
- `CategoryId` — represents a strongly typed category identifier;
- `CategoryType` — identifies whether a category classifies income or expense.

## Category properties

A category currently contains:

- a stable `CategoryId`;
- a non-empty user-visible name;
- a supported `CategoryType`;
- a UTC creation timestamp.

## Category invariants

A valid category must satisfy the following rules:

- its identifier must not be empty;
- its name must not be null, empty, or whitespace;
- its name is trimmed before it is stored;
- its type must be declared in `CategoryType`;
- its creation timestamp must not have the default value;
- its creation timestamp is stored in UTC.

Category instances are created through `Category.Create`. The constructor is private so that callers cannot bypass domain validation.

## Supported category types

- `Income` — classifies money received by the user;
- `Expense` — classifies money spent by the user.

The numeric value `0` is intentionally not assigned to a category type. The category entity rejects default and otherwise undefined `CategoryType` values.

Examples of category instances may include groceries, transport, subscriptions, health, entertainment, salary, and savings. These examples are category instances, while `CategoryType` contains only the stable directions `Income` and `Expense`.

## Not included yet

The current model intentionally does not contain:

- icons;
- colors;
- parent-child hierarchy;
- budget limits;
- user ownership;
- update operations or `UpdatedAt`;
- persistence mappings;
- Entity Framework Core attributes;
- API contracts or endpoints.

Persistence details, database mappings, and API contracts must not be placed in the domain model.
