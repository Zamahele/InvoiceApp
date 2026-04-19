# Project Guidelines

## Build and Test
- Restore: dotnet restore InvoiceApp.sln
- Build: dotnet build InvoiceApp.sln --configuration Release --no-restore
- Run web app: dotnet run --project InvoiceApp.Web
- Run tests: dotnet test InvoiceApp.Tests/InvoiceApp.Tests.csproj
- Run one test class: dotnet test InvoiceApp.Tests/InvoiceApp.Tests.csproj --filter FullyQualifiedName~InvoiceApp.Tests.Calculations
- Add migration from solution root: dotnet ef migrations add <MigrationName> --project InvoiceApp.Infrastructure --startup-project InvoiceApp.Web
- Apply migrations manually: dotnet ef database update --project InvoiceApp.Infrastructure --startup-project InvoiceApp.Web

## Architecture
- Keep clean architecture boundaries:
  - InvoiceApp.Core: entities and domain model only.
  - InvoiceApp.Infrastructure: EF Core DbContext, migrations, PDF service.
  - InvoiceApp.Web: ASP.NET Core Razor Pages, DI composition, startup behavior.
  - InvoiceApp.Tests: xUnit tests for calculations, data behavior, and PDF generation.
- Follow dependency direction: Web -> Infrastructure -> Core.

## Conventions
- Database schema is blacktech.
- Preserve decimal precision mapping in EF model:
  - Money fields: decimal(18,2)
  - Quantity fields: decimal(18,4)
  - Percentage/rate fields: decimal(5,2)
- QuestPDF must use Community license before PDF generation code paths (app startup and tests).
- Migrations are auto-applied at web startup; keep migration changes compatible with startup migration execution.
- Settings singleton pattern uses Id = 1 for CompanySettings and BankingDetails.
- Invoice numbering pattern is INV-### (for example INV-001).

## Common Pitfalls
- Do not hardcode production secrets. Production configuration uses token placeholders replaced by CI/CD.
- Development DB connection details may be environment-specific; verify appsettings.Development.json or user secrets before running.
- Keep invoice total calculations and rounding behavior aligned with existing tests.
- When changing entity relationships, verify cascade delete behavior in data tests.

## Key References
- Full project guidance: [CLAUDE.md](CLAUDE.md)
- App startup and migration behavior: [InvoiceApp.Web/Program.cs](InvoiceApp.Web/Program.cs)
- EF model mappings and schema: [InvoiceApp.Infrastructure/Data/AppDbContext.cs](InvoiceApp.Infrastructure/Data/AppDbContext.cs)
- Core calculation expectations: [InvoiceApp.Tests/Calculations/InvoiceTotalsTests.cs](InvoiceApp.Tests/Calculations/InvoiceTotalsTests.cs)
- PDF generation implementation: [InvoiceApp.Infrastructure/Services/InvoicePdfService.cs](InvoiceApp.Infrastructure/Services/InvoicePdfService.cs)
- Deployment pipeline and token replacement: [.github/workflows/deploy.yml](.github/workflows/deploy.yml)
