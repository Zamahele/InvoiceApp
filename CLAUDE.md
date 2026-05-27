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

Clean architecture with 4 projects. The app serves **two distinct domains** behind one deployment: **invoicing** and **monthly rent tracking**.

- **InvoiceApp.Core** - Domain entities only, no dependencies (`net8.0`, nullable + implicit usings enabled). Entities:
  - Tenant root: `Company` (the registered tenant). `ITenantOwned` (in the `InvoiceApp.Core` namespace) marks entities scoped to a company.
  - Invoicing: `Invoice`, `LineItem`, `CompanySettings`, `BankingDetails`, `SavedRate`.
  - Rent tracking: `Property` → `Room` → `RentPayment`.
  - Cross-cutting: `ErrorLog` (not tenant-owned). `RentSettings` is a dead entity (table was dropped by `AddPropertyAndLinkRooms`, no DbSet/usages).
- **InvoiceApp.Infrastructure** - EF Core `AppDbContext` (SQL Server, schema `blacktech`), migrations, ASP.NET Core Identity (`ApplicationUser` in `Identity/`), the `ICurrentCompanyProvider` tenancy abstraction (`Tenancy/`), and two QuestPDF v2024.3.4 (Community) services: `InvoicePdfService` and `RentReceiptPdfService`.
- **InvoiceApp.Web** - ASP.NET Core 8 Razor Pages. Auto-runs EF migrations on startup via `Program.cs` (guarded by `!EF.IsDesignTime`). Both PDF services registered as scoped. Ships as a PWA (`wwwroot/manifest.json`, `wwwroot/js/sw.js`, `/Offline` fallback page). Tenancy wiring (current-company provider, claims factory, service-gating filter) lives in `Tenancy/`.
- **InvoiceApp.Tests** - xUnit. Uses EF InMemory provider for DB/page tests; `Microsoft.AspNetCore.Mvc.Testing` for integration tests. Tests are grouped by area: `Calculations/`, `Data/`, `Pages/`, `Services/`.

## Multi-tenancy & auth

The app is multi-tenant: **one login = one `Company`** (the account *is* the tenant). All tenant data is isolated per company.

- **Auth:** ASP.NET Core Identity (`ApplicationUser : IdentityUser` with a `CompanyId` FK). Registration at `/Account/Register` creates the `Company` (with its enabled services) and the owning user, then signs in. `/Account/Login`, `/Account/Logout`. All pages require auth via `AuthorizeFolder("/")`; `/Account/*`, `/Error`, `/Offline`, `/Privacy` are `AllowAnonymous`.
- **Tenant scoping:** every `ITenantOwned` entity carries a `CompanyId`. `AppDbContext` applies an **EF global query filter** keyed on `_companyId` and **auto-stamps `CompanyId` on insert** in `SaveChanges`. The current company comes from `ICurrentCompanyProvider` (web impl reads the `CompanyId` claim, added on every sign-in by `AppUserClaimsPrincipalFactory`). When `_companyId` is null (startup migrations, unit tests constructing the context without a provider) the filters are bypassed — this is why existing single-tenant tests still pass.
- **IMPORTANT — never use `Find`/`FindAsync` for tenant-owned entities:** they bypass global query filters and let one company reach another's row by id. Use filtered `FirstOrDefaultAsync(x => x.Id == id)` instead.
- **Service selection:** `Company.InvoicingEnabled` / `RentTrackingEnabled` are chosen at registration and toggled at `/Settings` (the `Services` handler). `RequireServiceFilter` (applied to the `/Invoices` and `/Rent` folders via Razor Pages conventions in `Program.cs`) redirects to `/` if the feature isn't enabled; `_Layout` and the dashboard hide nav/cards for disabled services.

## Key Patterns

**Database:** SQL Server with default schema `blacktech` (set via `HasDefaultSchema` in `AppDbContext.OnModelCreating`). Migrations are auto-applied at startup in `Program.cs` (`db.Database.Migrate()`) — no manual `database update` needed in normal flow. Decimal precision: `(18,2)` for amounts, `(18,4)` for line-item quantities, `(5,2)` for rates/percentages. Relationship deletes: `Invoice→LineItem` cascade, `Room→RentPayment` cascade, `Property→Room` set-null.

**Rent tracking flow:** `/Rent/Index` is period-scoped by `Month`/`Year` query params. On each GET it calls `EnsurePaymentsForMonth()`, which lazily materializes a `RentPayment` row (seeded from `Room.RentAmount`) for every active room that lacks one for that period — so payment records are created on demand, not by a scheduled job. Mark-paid/undo are POST handlers that validate `amountPaid` against `AmountDue`. Receipts: `/Rent/ReceiptPreview` → `/Rent/DownloadReceipt` via `RentReceiptPdfService`. Rooms are managed at `/Settings/Rooms`.

**Invoicing flow:** `Create` → `Preview` → `DownloadPdf`. History at `/Invoices/History`. Settings (company info, banking details, saved rates) at `/Settings/`. Invoices support VAT, retention (Deposit/Holdback), reissue, and a `Category` field.

**Error logging:** Production exceptions route to `/Error` (`UseExceptionHandler`). `ErrorModel.OnGetAsync` persists an `ErrorLog` row (path, request id, message, stack, inner message) to the DB, wrapped in try/catch so a logging failure never replaces the user-facing error page. Logs are viewable at `/Admin/ErrorLog`.

**PDF Generation:** QuestPDF license is set to `Community` once at startup in `Program.cs`. Both PDF services live in `InvoiceApp.Infrastructure/Services/`.

## Configuration

- `appsettings.json` — development, points to `localhost` SQL Server (`InvoiceAppDev` DB, Windows auth).
- `appsettings.Production.json` — uses token placeholders `#{DB_SERVER}#`, `#{DB_USERNAME}#`, `#{DB_PASSWORD}#` replaced by the CI/CD pipeline.
- `web.config` — IIS in-process hosting for production.

## CI/CD

GitHub Actions workflow at `.github/workflows/deploy.yml`. Triggers on push/PR to `main`/`master` and manual dispatch. Three jobs: **build** (restore → build → `dotnet test` with `.trx` results uploaded as an artifact), **publish** (token substitution happens here — see below), and **ftp-deploy** (only on `main`/`master`). Deploy puts the site in maintenance mode by FTP-uploading `app_offline.htm` first and removes it afterward (`if: always()`), then deploys to `blacktech.gcweproperty.co.za/wwwroot/`. Token substitution replaces `#{DB_*}#` placeholders in `appsettings.Production.json` during the deploy job's "Prepare deployment files" step. Migrations are **not** run by the pipeline — they auto-apply on app startup (`Program.cs`).

Secrets required: `DB_SERVER`, `DB_USERNAME`, `DB_PASSWORD`, `FTP_SERVER`, `FTP_USERNAME`, `FTP_PASSWORD`.
