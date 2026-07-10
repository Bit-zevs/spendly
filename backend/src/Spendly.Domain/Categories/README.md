# Categories

This folder contains the category-related domain model.

Current contents:

- `CategoryId` — a strongly typed identifier for a category;
- `CategoryType` — identifies whether a category represents income or expense.

Possible future contents:

- category aggregate or entity;
- category name value object;
- category hierarchy rules;
- category-related domain errors.

Categories are used to classify income and expenses.

Examples may include food, transport, subscriptions, health, entertainment, salary, and savings. These examples are category instances, while `CategoryType` contains only the stable directions `Income` and `Expense`.

API request/response contracts and database mappings should not be placed here.
