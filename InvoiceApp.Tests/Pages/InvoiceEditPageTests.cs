using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using InvoiceApp.Infrastructure.Tenancy;
using InvoiceApp.Web.Pages.Invoices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Tests.Pages;

public class InvoiceEditPageTests
{
    private sealed class StubCompany : ICurrentCompanyProvider
    {
        public int? CompanyId { get; init; }
    }

    private static AppDbContext CreateDb(string name, int companyId) => new(
        new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(name).Options,
        new StubCompany { CompanyId = companyId });

    private static CreateModel CreateModel(AppDbContext db)
    {
        var model = new CreateModel(db);
        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
        model.PageContext = new PageContext(actionContext)
        {
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), modelState)
        };
        return model;
    }

    [Fact]
    public async Task OnGet_WithInvoiceId_LoadsInvoiceAndLineItemsForEditing()
    {
        const int companyId = 7;
        using var db = CreateDb(Guid.NewGuid().ToString(), companyId);
        var invoice = new Invoice
        {
            CompanyId = companyId,
            InvoiceNumber = "INV-014",
            InvoiceDate = new DateTime(2026, 4, 12),
            ClientName = "Current Client",
            LineItems = new List<LineItem>
            {
                new() { Description = "Painting", Unit = "m2", Quantity = 4, Rate = 25, Amount = 100 }
            }
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var model = CreateModel(db);
        var result = await model.OnGetAsync(invoice.Id);

        Assert.IsType<PageResult>(result);
        Assert.True(model.IsEditing);
        Assert.Equal(invoice.Id, model.Invoice.Id);
        Assert.Equal("INV-014", model.NextInvoiceNumber);
        Assert.Single(model.Items);
        Assert.Equal("Painting", model.Items[0].Description);
        Assert.Equal(4, model.Items[0].Quantity);
    }

    [Fact]
    public async Task OnPost_EditingUpdatesInvoiceAndPreservesUneditableSettings()
    {
        const int companyId = 9;
        using var db = CreateDb(Guid.NewGuid().ToString(), companyId);
        var createdAt = new DateTime(2026, 2, 1, 10, 30, 0);
        var invoice = new Invoice
        {
            CompanyId = companyId,
            InvoiceNumber = "INV-008",
            InvoiceDate = new DateTime(2026, 2, 1),
            ClientName = "Before Edit",
            VATEnabled = true,
            VATRate = 15,
            VATAmount = 15,
            RetentionEnabled = true,
            RetentionType = "Holdback",
            RetentionPercentage = 10,
            RetentionAmount = 10,
            SubTotal = 100,
            TotalAmount = 105,
            CreatedAt = createdAt,
            LineItems = new List<LineItem>
            {
                new() { Description = "Old work", Unit = "hour", Quantity = 2, Rate = 50, Amount = 100 }
            }
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var model = CreateModel(db);
        model.Invoice = new Invoice
        {
            Id = invoice.Id,
            InvoiceNumber = "INV-008",
            InvoiceDate = new DateTime(2026, 2, 2),
            ClientName = "After Edit",
            ClientEmail = "client@example.com",
            Category = "Maintenance",
            Notes = "Updated details"
        };
        model.Items = new List<CreateModel.LineItemInput>
        {
            new() { Description = "New work", Unit = "hour", Quantity = 3, Rate = 50 }
        };

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Invoices/Preview", redirect.PageName);
        Assert.Equal(invoice.Id, redirect.RouteValues!["id"]);

        var saved = await db.Invoices.Include(i => i.LineItems).SingleAsync();
        Assert.Equal("After Edit", saved.ClientName);
        Assert.Equal("client@example.com", saved.ClientEmail);
        Assert.Equal("Maintenance", saved.Category);
        Assert.Equal("Updated details", saved.Notes);
        Assert.Equal(companyId, saved.CompanyId);
        Assert.Equal(createdAt, saved.CreatedAt);
        Assert.True(saved.VATEnabled);
        Assert.Equal(15, saved.VATRate);
        Assert.Equal(22.5m, saved.VATAmount);
        Assert.True(saved.RetentionEnabled);
        Assert.Equal("Holdback", saved.RetentionType);
        Assert.Equal(15m, saved.RetentionAmount);
        Assert.Equal(157.5m, saved.TotalAmount);
        Assert.Single(saved.LineItems);
        Assert.Equal("New work", saved.LineItems[0].Description);
    }

    [Fact]
    public async Task OnPost_BlankQuantityAndRateReturnFieldSpecificErrors()
    {
        const int companyId = 11;
        using var db = CreateDb(Guid.NewGuid().ToString(), companyId);
        var invoice = new Invoice
        {
            CompanyId = companyId,
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Today,
            ClientName = "Client"
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var model = CreateModel(db);
        model.Invoice = new Invoice
        {
            Id = invoice.Id,
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Today,
            ClientName = "Client"
        };
        model.Items = new List<CreateModel.LineItemInput>
        {
            new() { Description = "Work", Quantity = null, Rate = null }
        };

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains(model.ModelState["Items[0].Quantity"]!.Errors,
            error => error.ErrorMessage == "Quantity must be greater than 0.");
        Assert.Contains(model.ModelState["Items[0].Rate"]!.Errors,
            error => error.ErrorMessage == "Rate is required and cannot be negative.");
        Assert.Empty(await db.LineItems.ToListAsync());
    }
}
