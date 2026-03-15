using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InvoiceApp.Tests.Data;

public class DbTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Can_Save_And_Retrieve_Invoice()
    {
        using var db = CreateInMemoryDb();
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-001",
            InvoiceDate = DateTime.Today,
            ClientName = "Test Client",
            SubTotal = 1_000m,
            TotalAmount = 1_000m
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var saved = await db.Invoices.FindAsync(invoice.Id);
        Assert.NotNull(saved);
        Assert.Equal("INV-001", saved.InvoiceNumber);
        Assert.Equal("Test Client", saved.ClientName);
    }

    [Fact]
    public async Task LineItems_Are_Cascade_Deleted_With_Invoice()
    {
        using var db = CreateInMemoryDb();
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-002",
            InvoiceDate = DateTime.Today,
            ClientName = "Client",
            LineItems = new List<LineItem>
            {
                new() { Description = "Paving", Unit = "m2", Quantity = 10, Rate = 100, Amount = 1000 }
            }
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        Assert.Equal(1, await db.LineItems.CountAsync());

        db.Invoices.Remove(invoice);
        await db.SaveChangesAsync();

        Assert.Equal(0, await db.LineItems.CountAsync());
    }

    [Fact]
    public async Task Can_Save_And_Retrieve_CompanySettings()
    {
        using var db = CreateInMemoryDb();
        var settings = new CompanySettings
        {
            Id = 1,
            Name = "Lifestar Builders",
            Phone = "066 378 7872"
        };
        db.CompanySettings.Add(settings);
        await db.SaveChangesAsync();

        var saved = await db.CompanySettings.FindAsync(1);
        Assert.NotNull(saved);
        Assert.Equal("Lifestar Builders", saved.Name);
    }

    [Fact]
    public async Task Can_Save_And_Retrieve_BankingDetails()
    {
        using var db = CreateInMemoryDb();
        var banking = new BankingDetails
        {
            Id = 1,
            BankName = "Capitec Bank",
            AccountType = "Savings",
            AccountNumber = "1762812922"
        };
        db.BankingDetails.Add(banking);
        await db.SaveChangesAsync();

        var saved = await db.BankingDetails.FindAsync(1);
        Assert.NotNull(saved);
        Assert.Equal("Capitec Bank", saved.BankName);
        Assert.Equal("1762812922", saved.AccountNumber);
    }

    [Fact]
    public async Task Can_Save_And_Retrieve_SavedRate()
    {
        using var db = CreateInMemoryDb();
        db.SavedRates.Add(new SavedRate { Description = "Paving", Unit = "m2", Rate = 120m });
        db.SavedRates.Add(new SavedRate { Description = "Labour", Unit = "hours", Rate = 250m });
        await db.SaveChangesAsync();

        var rates = await db.SavedRates.ToListAsync();
        Assert.Equal(2, rates.Count);
    }

    [Fact]
    public async Task Invoice_Number_Auto_Increment_Logic()
    {
        using var db = CreateInMemoryDb();
        db.Invoices.Add(new Invoice { InvoiceNumber = "INV-001", InvoiceDate = DateTime.Today, ClientName = "A" });
        db.Invoices.Add(new Invoice { InvoiceNumber = "INV-002", InvoiceDate = DateTime.Today, ClientName = "B" });
        await db.SaveChangesAsync();

        var last = await db.Invoices.OrderByDescending(i => i.Id).FirstOrDefaultAsync();
        Assert.NotNull(last);
        var parts = last.InvoiceNumber.Split('-');
        Assert.Equal(2, int.Parse(parts[1]));

        int next = int.Parse(parts[1]) + 1;
        Assert.Equal("INV-003", $"INV-{next:D3}");
    }

    [Fact]
    public async Task Multiple_Invoices_Retrieved_In_Descending_Date_Order()
    {
        using var db = CreateInMemoryDb();
        db.Invoices.Add(new Invoice { InvoiceNumber = "INV-001", InvoiceDate = new DateTime(2026, 1, 1), ClientName = "A" });
        db.Invoices.Add(new Invoice { InvoiceNumber = "INV-002", InvoiceDate = new DateTime(2026, 3, 1), ClientName = "B" });
        db.Invoices.Add(new Invoice { InvoiceNumber = "INV-003", InvoiceDate = new DateTime(2026, 2, 1), ClientName = "C" });
        await db.SaveChangesAsync();

        var ordered = await db.Invoices.OrderByDescending(i => i.InvoiceDate).ToListAsync();
        Assert.Equal("INV-002", ordered[0].InvoiceNumber);
        Assert.Equal("INV-003", ordered[1].InvoiceNumber);
        Assert.Equal("INV-001", ordered[2].InvoiceNumber);
    }
}
