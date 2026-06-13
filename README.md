# MiMealMoney

**MiChoice MiMealMoney** — parent online cafeteria account payment portal.

Parents use MiMealMoney to deposit funds into their child's school cafeteria account, view balances, and receive proactive low-balance alerts via miAgentic.

---

## What This Is

MiMealMoney is a standalone Blazor Server app — separate from `michoice-central-office` and `michoice-parent-portal`. It is purely about account funding and balance visibility. It is **not** the Free & Reduced application portal (that's `michoice-parent-portal`).

---

## Features

- Parent registration and login (ASP.NET Core Identity)
- Link multiple children to a single household account
- View current balance per student with low-balance indicator
- Make payments via credit/debit card (Stripe — stub, UI wired)
- miAgentic balance insight: projects days remaining from average daily spend
- Transaction history across all linked students (paginated)
- Account settings: name, language (English/Spanish), notification preferences, per-child alert threshold
- Clean light theme — white background, green primary `#1a6b3a`, Bootstrap 5

---

## Solution Structure

```
MiChoice.MealMoney.sln
├── src/MiChoice.MealMoney.Domain/        — entities, enums (no external deps)
├── src/MiChoice.MealMoney.Infrastructure/ — EF Core, Identity, Stripe stub, miAgentic
└── src/MiChoice.MealMoney.Web/            — Blazor Server app
```

---

## Stack

- **.NET 9** — Blazor Server
- **Entity Framework Core 9** — SQLite (dev) / SQL Server (prod)
- **ASP.NET Core Identity** — parent user accounts
- **Stripe.net** — payment processing (stubbed; wire `Stripe:SecretKey` in appsettings to activate)
- **Bootstrap 5** — CDN, light theme

---

## Running Locally

```bash
cd src/MiChoice.MealMoney.Web
dotnet run
```

The app uses SQLite by default (`miMealMoney.db`, created automatically). Set `DatabaseProvider: "SqlServer"` in `appsettings.Production.json` and supply a connection string for production.

---

## Stripe Wiring

Replace the placeholder keys in `appsettings.json`:

```json
"Stripe": {
  "PublishableKey": "pk_live_...",
  "SecretKey": "sk_live_..."
}
```

Then replace `StripePaymentService` stub logic with real `PaymentIntentService` calls.

---

## miAgentic

`MealMoneyInsightService` is a pure-math projection — no external API call. It calculates average daily spend from recent transactions and projects days to zero balance. Severity levels: Info / Warning / Action.

---

*Built by V — CIO, Carborundum AI*  
*Part of the MiChoice platform*
