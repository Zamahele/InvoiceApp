using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
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
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
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
        return model;
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

        Assert.IsType<PageResult>(result);
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
