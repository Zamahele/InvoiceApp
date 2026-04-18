# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Restore dependencies
dotnet restore InvoiceApp.sln

# Build
dotnet build InvoiceApp.sln

# Run (development, HTTP on port 5000)
dotnet run --project InvoiceApp.Web

# Run all tests
dotnet test InvoiceApp.Tests/InvoiceApp.Tests.csproj

# Run a single test class
dotnet test InvoiceApp.Tests/InvoiceApp.Tests.csproj --filter "FullyQualifiedName~InvoiceApp.Tests.Calculations"

# Add a new EF migration (run from solution root)
dotnet ef migrations add <MigrationName> --project InvoiceApp.Infrastructure --startup-project InvoiceApp.Web

# Apply migrations manually
dotnet ef database update --project InvoiceApp.Infrastructure --startup-project InvoiceApp.Web
```

## Architecture

Clean architecture with 4 projects:

- **InvoiceApp.Core** - Domain entities only, no dependencies. Entities: `Invoice`, `LineItem`, `CompanySettings`, `BankingDetails`, `SavedRate`.
- **InvoiceApp.Infrastructure** - EF Core `AppDbContext` (SQL Server, schema `blacktech`), migrations, and `InvoicePdfService` (QuestPDF v2024.3.4 Community).
- **InvoiceApp.Web** - ASP.NET Core 8 Razor Pages. Auto-runs EF migrations on startup via `Program.cs`. Registers `InvoicePdfService` as scoped.
- **InvoiceApp.Tests** - xUnit tests. Uses EF InMemory provider for DB tests; `Microsoft.AspNetCore.Mvc.Testing` for integration tests.

## Key Patterns

**Database:** SQL Server with schema `blacktech`. `AppDbContext` in `InvoiceApp.Infrastructure/Data/`. Migrations are auto-applied at startup in `Program.cs`. Decimals: `(18,2)` for amounts, `(18,4)` for quantities, `(5,2)` for rates.

**PDF Generation:** `InvoicePdfService` uses QuestPDF. The license is set to `Community` at startup. The service is in `InvoiceApp.Infrastructure/Services/`.

**Razor Pages flow:** `Create` → `Preview` → `DownloadPdf`. History at `/Invoices/History`. Settings (company info, banking details, saved rates) at `/Settings/`.

## Configuration

- `appsettings.json` — development, points to `localhost` SQL Server (`InvoiceAppDev` DB, Windows auth).
- `appsettings.Production.json` — uses token placeholders `#{DB_SERVER}#`, `#{DB_USERNAME}#`, `#{DB_PASSWORD}#` replaced by the CI/CD pipeline.
- `web.config` — IIS in-process hosting for production.

## CI/CD

GitHub Actions workflow at `.github/workflows/deploy.yml`. Triggers on push to `master`. Pipeline: build → publish (with token substitution for DB credentials) → FTP deploy to `blacktech.gcweproperty.co.za/wwwroot/`.

Secrets required: `DB_SERVER`, `DB_USERNAME`, `DB_PASSWORD`, `FTP_SERVER`, `FTP_USERNAME`, `FTP_PASSWORD`.
