using InvoiceApp.Infrastructure.Data;
using InvoiceApp.Infrastructure.Identity;
using InvoiceApp.Infrastructure.Tenancy;
using InvoiceApp.Web.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
});

// Tenant resolution: current company id (from the user's claim) + the loaded Company.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentCompanyProvider, CurrentCompanyProvider>();
builder.Services.AddScoped<CompanyContext>();

builder.Services.AddScoped<InvoiceApp.Infrastructure.Services.InvoicePdfService>();
builder.Services.AddScoped<InvoiceApp.Infrastructure.Services.RentReceiptPdfService>();

builder.Services.AddRazorPages(options =>
{
    // Everything requires a signed-in company except the auth + error pages.
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToFolder("/Account");
    options.Conventions.AllowAnonymousToPage("/Error");
    options.Conventions.AllowAnonymousToPage("/Offline");
    options.Conventions.AllowAnonymousToPage("/Privacy");

    // Feature gating: only companies that enabled a service can reach its pages.
    options.Conventions.AddFolderApplicationModelConvention("/Invoices",
        model => model.Filters.Add(new RequireServiceFilter(Service.Invoicing)));
    options.Conventions.AddFolderApplicationModelConvention("/Rent",
        model => model.Filters.Add(new RequireServiceFilter(Service.RentTracking)));
});

var app = builder.Build();

// Apply pending migrations on startup (skipped under EF design-time tooling).
if (!EF.IsDesignTime)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // One-time cutover: wipe the legacy single-tenant schema if present so the
        // multi-tenant migration can apply cleanly. Self-disables after first success.
        LegacySchemaReset.ResetIfLegacySchemaPresent(db, logger);

        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        // Startup DB failures otherwise surface only as an opaque HTTP 500.30.
        // Write the full exception to a retrievable file (logs/ is FTP-only) and
        // log it, then rethrow so the failure is not silently swallowed.
        logger.LogCritical(ex, "Startup database initialization failed.");
        try
        {
            var logDir = Path.Combine(app.Environment.ContentRootPath, "logs");
            Directory.CreateDirectory(logDir);
            File.WriteAllText(
                Path.Combine(logDir, "startup-error.log"),
                $"{DateTime.UtcNow:o}{Environment.NewLine}{ex}");
        }
        catch { /* never mask the original failure */ }
        throw;
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
