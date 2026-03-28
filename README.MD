# PersonalCash

PersonalCash is a manual personal finance tracker for people who want clarity, control, and simple monthly planning without bank sync, hidden automation, or overloaded workflows.

It started as an Excel-based personal budgeting workflow and grew into a web app that keeps the same feeling of manual control while adding mobile access, cleaner input flows, monthly statistics, and a separate debts section.

## Why PersonalCash exists

Many finance apps feel powerful, but also noisy, overbuilt, and strangely distant from the person using them.

PersonalCash was built from a different idea:

- manual control over every number
- clear separation between actual and planned transactions
- useful monthly statistics without analytical overload
- a separate debts section that stays visible without dominating the main flow
- a calmer UI that feels closer to a structured spreadsheet than to a financial platform

## Who it is for

PersonalCash is for people who:

- dislike overloaded finance apps
- want to understand every number themselves
- prefer manual entry over aggressive automation
- used spreadsheets for budgeting and want a better mobile-friendly interface
- want simple monthly/category statistics to plan the next month more calmly

## Core ideas

- **Manual first** — the app helps you track money, not think instead of you
- **Actual vs planned** — future operations stay separate from completed ones
- **Statistics without noise** — reports are meant for everyday planning, not dashboard theater
- **Debts as a side system** — visible and useful, but not mixed into daily spending
- **Calm interface** — simplicity is intentional, not accidental

## Current features

- accounts
- categories
- transactions
- planned transactions
- monthly/category reports
- debts and debt payments
- localization
- responsive UI for desktop and mobile
- Blazor WebAssembly frontend with Supabase backend

## Screenshots

### Home
![Home](docs/images/home.png)

### Transactions
![Transactions](docs/images/transactions.png)

### Reports
![Reports](docs/images/reports.png)

### Debts
![Debts](docs/images/debts.png)

> Screenshots are being updated to match the current UI.

## Tech stack

- Blazor WebAssembly (.NET 10)
- MudBlazor v9
- Supabase (PostgREST + Row Level Security)
- no custom API

## Architecture

The solution is split into four layers:

- **PersonalCash** — UI client, pages, components, DI, localization, shared UI utilities
- **Domain** — DTOs, contracts, ports, enums, constants
- **Application** — business services
- **Infrastructure** — Supabase models, mappings, repositories, auth/database/client infrastructure

Flow:

UI -> Application -> Domain ports -> Infrastructure

## Getting started

### Requirements

- .NET 10 SDK
- Supabase project
- environment/config values for Supabase

### Run locally

```bash
dotnet watch run
```

## Project status

PersonalCash is an actively evolving personal project.

The current focus is not feature bloat, but strengthening the core idea:
manual control, calm UI, and useful everyday planning.