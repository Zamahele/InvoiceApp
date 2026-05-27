using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using InvoiceApp.Infrastructure.Tenancy;
using InvoiceApp.Web.Pages.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InvoiceApp.Tests.Pages;

public class SettingsPageTests
{
    private sealed class StubCompany : ICurrentCompanyProvider
    {
        public int? CompanyId { get; init; }
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // A context scoped to a signed-in tenant, mirroring the real request pipeline
    // (the web layer injects an ICurrentCompanyProvider built from the user's claim).
    private static AppDbContext CreateDb(string dbName, int companyId)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options, new StubCompany { CompanyId = companyId });
    }

    private static IndexModel CreateModel(AppDbContext db)
    {
        var model = new IndexModel(db);
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), modelState);
        var pageContext = new PageContext(actionContext) { ViewData = viewData };
        model.PageContext = pageContext;
        model.TempData = new FakeTempData();
        return model;
    }

    // The Services handler resolves the tenant from the signed-in user's CompanyId
    // claim, so a tenant-scoped test must put that claim on the principal.
    private static IndexModel CreateModel(AppDbContext db, int companyId)
    {
        var model = CreateModel(db);
        var identity = new System.Security.Claims.ClaimsIdentity(
            new[] { new System.Security.Claims.Claim("CompanyId", companyId.ToString()) }, "Test");
        model.PageContext.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(identity);
        return model;
    }

    private class FakeTempData : Dictionary<string, object?>, ITempDataDictionary
    {
        public void Keep() { }
        public void Keep(string key) { }
        public void Load() { }
        public object? Peek(string key) => TryGetValue(key, out var v) ? v : null;
        public void Save() { }
    }

    [Fact]
    public async Task OnPost_Inserts_When_No_Existing_Records()
    {
        using var db = CreateDb();
        var model = CreateModel(db);
        model.Company = new CompanySettings { Name = "Lifestar Builders" };
        model.Banking = new BankingDetails
        {
            BankName = "Capitec",
            AccountType = "Savings",
            AccountNumber = "1234567890",
            BranchCode = "470010"
        };

        var result = await model.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(1, await db.CompanySettings.CountAsync());
        Assert.Equal(1, await db.BankingDetails.CountAsync());
        var saved = await db.CompanySettings.FirstOrDefaultAsync();
        Assert.Equal("Lifestar Builders", saved!.Name);
    }

    [Fact]
    public async Task OnPost_Updates_Existing_Records()
    {
        using var db = CreateDb();
        db.CompanySettings.Add(new CompanySettings { Name = "Old Name", Phone = "000" });
        db.BankingDetails.Add(new BankingDetails
        {
            BankName = "Old Bank",
            AccountType = "Savings",
            AccountNumber = "111",
            BranchCode = "000"
        });
        await db.SaveChangesAsync();

        var model = CreateModel(db);
        model.Company = new CompanySettings { Name = "New Name", Phone = "082 000 0000" };
        model.Banking = new BankingDetails
        {
            BankName = "New Bank",
            AccountType = "Cheque",
            AccountNumber = "999",
            BranchCode = "123"
        };

        await model.OnPostAsync();

        var company = await db.CompanySettings.FirstOrDefaultAsync();
        Assert.Equal("New Name", company!.Name);
        Assert.Equal("082 000 0000", company.Phone);

        var banking = await db.BankingDetails.FirstOrDefaultAsync();
        Assert.Equal("New Bank", banking!.BankName);
        Assert.Equal("Cheque", banking.AccountType);
        Assert.Equal("999", banking.AccountNumber);
    }

    [Fact]
    public async Task OnPost_Returns_Page_With_Error_When_CompanyName_Missing()
    {
        using var db = CreateDb();
        var model = CreateModel(db);
        model.Company = new CompanySettings { Name = "" };
        model.Banking = new BankingDetails
        {
            BankName = "Capitec",
            AccountType = "Savings",
            AccountNumber = "123",
            BranchCode = "470010"
        };

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey("Company.Name"));
        Assert.Equal(0, await db.CompanySettings.CountAsync());
    }

    [Fact]
    public async Task OnPost_Returns_Page_With_Error_When_Banking_Fields_Missing()
    {
        using var db = CreateDb();
        var model = CreateModel(db);
        model.Company = new CompanySettings { Name = "Lifestar" };
        model.Banking = new BankingDetails { BankName = "", AccountType = "", AccountNumber = "", BranchCode = "" };

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey("Banking.BankName"));
        Assert.Equal(0, await db.BankingDetails.CountAsync());
    }

    [Fact]
    public async Task OnPost_Stamps_New_Records_With_Current_Company()
    {
        const int companyId = 42;
        var dbName = Guid.NewGuid().ToString();
        using var db = CreateDb(dbName, companyId);
        db.Companies.Add(new Company { Id = companyId, Name = "Tenant Co", InvoicingEnabled = true });
        await db.SaveChangesAsync();

        var model = CreateModel(db);
        model.Company = new CompanySettings { Name = "Lifestar Builders", Phone = "082 000 0000" };
        model.Banking = new BankingDetails
        {
            BankName = "Capitec",
            AccountType = "Savings",
            AccountNumber = "1234567890",
            BranchCode = "470010"
        };

        var result = await model.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var savedCompany = await db.CompanySettings.SingleAsync();
        var savedBanking = await db.BankingDetails.SingleAsync();
        Assert.Equal(companyId, savedCompany.CompanyId);
        Assert.Equal(companyId, savedBanking.CompanyId);
        Assert.Equal("Lifestar Builders", savedCompany.Name);
        Assert.Equal("Capitec", savedBanking.BankName);
    }

    [Fact]
    public async Task OnPost_Saves_Are_Isolated_Between_Companies()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var dbA = CreateDb(dbName, companyId: 1))
        {
            var modelA = CreateModel(dbA);
            modelA.Company = new CompanySettings { Name = "Company A" };
            modelA.Banking = new BankingDetails { BankName = "Bank A", AccountType = "Savings", AccountNumber = "111", BranchCode = "001" };
            await modelA.OnPostAsync();
        }

        using (var dbB = CreateDb(dbName, companyId: 2))
        {
            var modelB = CreateModel(dbB);
            modelB.Company = new CompanySettings { Name = "Company B" };
            modelB.Banking = new BankingDetails { BankName = "Bank B", AccountType = "Cheque", AccountNumber = "222", BranchCode = "002" };
            await modelB.OnPostAsync();

            // B's GET-style load sees only its own settings, never A's.
            var visible = await dbB.CompanySettings.SingleAsync();
            Assert.Equal("Company B", visible.Name);
            Assert.Equal(2, visible.CompanyId);
        }

        // Unscoped context confirms both rows actually persisted (one per company).
        using var unscoped = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(dbName).Options);
        Assert.Equal(2, await unscoped.CompanySettings.CountAsync());
    }

    [Fact]
    public async Task OnPostServices_Disabling_A_Service_Persists_False()
    {
        const int companyId = 5;
        var dbName = Guid.NewGuid().ToString();
        using var db = CreateDb(dbName, companyId);
        db.Companies.Add(new Company { Id = companyId, Name = "Co", InvoicingEnabled = true, RentTrackingEnabled = true });
        await db.SaveChangesAsync();

        var model = CreateModel(db, companyId);
        var result = await model.OnPostServicesAsync(enableInvoicing: false, enableRentTracking: true);

        Assert.IsType<RedirectToPageResult>(result);
        var company = await db.Companies.FindAsync(companyId);
        Assert.False(company!.InvoicingEnabled);
        Assert.True(company.RentTrackingEnabled);
    }

    [Fact]
    public async Task OnPostServices_Cannot_Disable_Every_Service()
    {
        const int companyId = 6;
        var dbName = Guid.NewGuid().ToString();
        using var db = CreateDb(dbName, companyId);
        db.Companies.Add(new Company { Id = companyId, Name = "Co", InvoicingEnabled = true, RentTrackingEnabled = true });
        await db.SaveChangesAsync();

        var model = CreateModel(db, companyId);
        await model.OnPostServicesAsync(enableInvoicing: false, enableRentTracking: false);

        // Guard rejects turning everything off; both flags stay as they were.
        var company = await db.Companies.FindAsync(companyId);
        Assert.True(company!.InvoicingEnabled);
        Assert.True(company.RentTrackingEnabled);
    }

    [Fact]
    public async Task OnPost_Does_Not_Create_Duplicate_Records_On_Repeated_Saves()
    {
        using var db = CreateDb();
        var model1 = CreateModel(db);
        model1.Company = new CompanySettings { Name = "First Save" };
        model1.Banking = new BankingDetails { BankName = "Bank", AccountType = "Savings", AccountNumber = "1", BranchCode = "1" };
        await model1.OnPostAsync();

        var model2 = CreateModel(db);
        model2.Company = new CompanySettings { Name = "Second Save" };
        model2.Banking = new BankingDetails { BankName = "Bank", AccountType = "Savings", AccountNumber = "1", BranchCode = "1" };
        await model2.OnPostAsync();

        Assert.Equal(1, await db.CompanySettings.CountAsync());
        Assert.Equal(1, await db.BankingDetails.CountAsync());
        var company = await db.CompanySettings.FirstOrDefaultAsync();
        Assert.Equal("Second Save", company!.Name);
    }
}
