using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using InvoiceApp.Infrastructure.Tenancy;
using InvoiceApp.Web.Pages.Settings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InvoiceApp.Tests.Pages;

public class EmailSettingsPageTests
{
    private sealed class StubCompany : ICurrentCompanyProvider
    {
        public int? CompanyId { get; init; }
    }

    private static AppDbContext CreateDb(string dbName, int companyId) =>
        new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(dbName).Options,
            new StubCompany { CompanyId = companyId });

    private static EmailModel CreateModel(AppDbContext db, IDataProtectionProvider dp)
    {
        var model = new EmailModel(db, dp);
        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), modelState);
        model.PageContext = new PageContext(actionContext) { ViewData = viewData };
        model.TempData = new FakeTempData();
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

    private static EmailModel.InputModel ValidInput(string? password) => new()
    {
        SmtpHost = "smtp.example.com",
        SmtpPort = 587,
        UseSsl = true,
        Username = "user@example.com",
        Password = password,
        FromEmail = "accounts@example.com",
        FromName = "Tenant Co"
    };

    [Fact]
    public async Task OnPost_Saves_New_Settings_With_Password_Encrypted_At_Rest()
    {
        const int companyId = 7;
        var dp = new EphemeralDataProtectionProvider();
        using var db = CreateDb(Guid.NewGuid().ToString(), companyId);
        var model = CreateModel(db, dp);
        model.Input = ValidInput("super-secret-pw");

        var result = await model.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var saved = await db.EmailSettings.SingleAsync();
        Assert.Equal(companyId, saved.CompanyId);            // tenant-stamped
        Assert.Equal("smtp.example.com", saved.SmtpHost);
        Assert.NotEqual("super-secret-pw", saved.Password);  // not plaintext
        Assert.False(string.IsNullOrEmpty(saved.Password));

        // Round-trips back to the original with the same purpose.
        var plain = dp.CreateProtector(EmailProtection.Purpose).Unprotect(saved.Password);
        Assert.Equal("super-secret-pw", plain);
    }

    [Fact]
    public async Task OnPost_First_Save_Requires_Password()
    {
        var dp = new EphemeralDataProtectionProvider();
        using var db = CreateDb(Guid.NewGuid().ToString(), companyId: 1);
        var model = CreateModel(db, dp);
        model.Input = ValidInput(password: null);

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey("Input.Password"));
        Assert.Equal(0, await db.EmailSettings.CountAsync());
    }

    [Fact]
    public async Task OnPost_Update_With_Blank_Password_Keeps_Existing_Password()
    {
        const int companyId = 3;
        var dp = new EphemeralDataProtectionProvider();
        var protector = dp.CreateProtector(EmailProtection.Purpose);
        var dbName = Guid.NewGuid().ToString();

        using (var seed = CreateDb(dbName, companyId))
        {
            seed.EmailSettings.Add(new EmailSettings
            {
                SmtpHost = "old.example.com",
                SmtpPort = 25,
                Username = "old@example.com",
                Password = protector.Protect("original-pw"),
                FromEmail = "old@example.com",
                FromName = "Old"
            });
            await seed.SaveChangesAsync();
        }

        using var db = CreateDb(dbName, companyId);
        var model = CreateModel(db, dp);
        model.Input = ValidInput(password: null);          // change host, leave password blank
        model.Input.SmtpHost = "new.example.com";

        var result = await model.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var saved = await db.EmailSettings.SingleAsync();   // still one row
        Assert.Equal("new.example.com", saved.SmtpHost);    // updated
        Assert.Equal("original-pw", protector.Unprotect(saved.Password)); // password preserved
    }
}
