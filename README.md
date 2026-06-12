# Spendly

Spendly is a personal finance assistant for expense tracking, budgeting, subscriptions, financial goals and daily safe spending calculation.

## Main idea

The main goal of Spendly is to help users understand how much money they can safely spend today without breaking their monthly budget.

## Planned features

- User accounts
- Wallets
- Income and expense tracking
- Categories
- Monthly budgets
- Daily safe spend calculation
- Subscriptions
- Financial goals
- Analytics
- Telegram bot
- Web application
- Mobile application later

## Tech stack

### Backend

- C#
- .NET 10
- ASP.NET Core Web API
- PostgreSQL
- Entity Framework Core
- Docker
- GitHub Actions

### Frontend

- React
- TypeScript
- Vite

### Mobile later

- React Native
- Expo

## Local development

### Requirements

- .NET 10 SDK
- Docker Desktop
- JetBrains Rider

### Build backend

```bash
cd backend
dotnet restore Spendly.sln
dotnet build Spendly.sln
dotnet test Spendly.sln
```

### Run PostgreSQL

```bash
docker compose -f deploy/docker-compose.yml up -d
```

## Project status

The project is in the initial development stage.
