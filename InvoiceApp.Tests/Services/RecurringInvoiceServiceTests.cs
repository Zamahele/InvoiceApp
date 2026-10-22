using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using InvoiceApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Tests.Services;

public class RecurringInvoiceServiceTests
{
    private static AppDbContext CreateDb() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task GenerateDueInvoices_CopiesTemplateOnceAndAdvancesSchedule()
    {
        using var db = CreateDb();
        var schedule = new RecurringInvoiceSchedule
        {
            CompanyId = 5,
            SourceInvoiceId = 18,
            ClientName = "Hosted Client",
            ClientEmail = "accounts@example.com",
            Category = "Hosting",
            VATEnabled = true,
            VATRate = 15,
            DueDay = 5,
            NextRunDate = new DateTime(2026, 7, 5),
            LineItems = new List<RecurringInvoiceLineItem>
            {
                new() { Description = "Hosting service", Unit = "monthly", Quantity = 1, Rate = 390, SortOrder = 0 }
            }
        };
        db.RecurringInvoiceSchedules.Add(schedule);
        await db.SaveChangesAsync();

        var service = new RecurringInvoiceService(db);
        var generated = await service.GenerateDueInvoicesAsync(new DateTime(2026, 7, 5));

        Assert.Equal(1, generated);
        var invoice = await db.Invoices.Include(item => item.LineItems).SingleAsync();
        Assert.Equal("INV-001", invoice.InvoiceNumber);
        Assert.Equal(schedule.Id, invoice.RecurringScheduleId);
        Assert.Equal(new DateTime(2026, 7, 1), invoice.RecurringPeriod);
        Assert.Equal(new DateTime(2026, 7, 5), invoice.InvoiceDate);
        Assert.Equal("Hosted Client", invoice.ClientName);
        Assert.Equal("accounts@example.com", invoice.ClientEmail);
        Assert.Equal("Hosting", invoice.Category);
        Assert.Equal(390m, invoice.SubTotal);
        Assert.Equal(58.5m, invoice.VATAmount);
        Assert.Equal(448.5m, invoice.TotalAmount);
        Assert.Single(invoice.LineItems);
        Assert.Equal("Hosting service", invoice.LineItems[0].Description);
        Assert.Equal(new DateTime(2026, 8, 5), schedule.NextRunDate);

        Assert.Equal(0, await service.GenerateDueInvoicesAsync(new DateTime(2026, 7, 5)));
        Assert.Single(await db.Invoices.ToListAsync());
    }

    [Fact]
    public async Task GenerateDueInvoices_CatchesUpMissedMonthsWithoutDriftingDueDay()
    {
        using var db = CreateDb();
        var schedule = new RecurringInvoiceSchedule
        {
            CompanyId = 3,
            SourceInvoiceId = 27,
            ClientName = "Client",
            DueDay = 31,
            NextRunDate = new DateTime(2026, 1, 31),
            LineItems = new List<RecurringInvoiceLineItem>
            {
                new() { Description = "Monthly hosting", Unit = "monthly", Quantity = 1, Rate = 100 }
            }
        };
        db.RecurringInvoiceSchedules.Add(schedule);
        await db.SaveChangesAsync();

        var service = new RecurringInvoiceService(db);
        var generated = await service.GenerateDueInvoicesAsync(new DateTime(2026, 3, 1));

        Assert.Equal(2, generated);
        var invoices = await db.Invoices.OrderBy(invoice => invoice.InvoiceDate).ToListAsync();
        Assert.Equal(new DateTime(2026, 1, 31), invoices[0].InvoiceDate);
        Assert.Equal(new DateTime(2026, 2, 28), invoices[1].InvoiceDate);
        Assert.Equal(new DateTime(2026, 3, 31), schedule.NextRunDate);
    }

    [Fact]
    public async Task GenerateDueInvoices_NumbersInvoicesWithinCompany()
    {
        using var db = CreateDb();
        db.Invoices.Add(new Invoice
        {
            CompanyId = 1,
            InvoiceNumber = "INV-041",
            InvoiceDate = new DateTime(2026, 1, 1),
            ClientName = "Other company"
        });
        db.Invoices.Add(new Invoice
        {
            CompanyId = 2,
            InvoiceNumber = "INV-006",
            InvoiceDate = new DateTime(2026, 1, 1),
            ClientName = "This company"
        });
        var schedule = new RecurringInvoiceSchedule
        {
            CompanyId = 2,
            SourceInvoiceId = 55,
            ClientName = "This company",
            DueDay = 10,
            NextRunDate = new DateTime(2026, 2, 10),
            LineItems = new List<RecurringInvoiceLineItem>
            {
                new() { Description = "Monthly service", Unit = "monthly", Quantity = 1, Rate = 50 }
            }
        };
        db.RecurringInvoiceSchedules.Add(schedule);
        await db.SaveChangesAsync();

        var service = new RecurringInvoiceService(db);
        await service.GenerateDueInvoicesAsync(new DateTime(2026, 2, 10));

        var generatedInvoice = await db.Invoices.SingleAsync(invoice => invoice.RecurringScheduleId == schedule.Id);
        Assert.Equal(2, generatedInvoice.CompanyId);
        Assert.Equal("INV-007", generatedInvoice.InvoiceNumber);
    }

    [Theory]
    [InlineData(2026, 1, 31, 31, 2026, 2, 28)]
    [InlineData(2026, 2, 28, 31, 2026, 3, 31)]
    public void GetNextRunDate_UsesSelectedMonthlyDay(int year, int month, int day, int dueDay, int expectedYear, int expectedMonth, int expectedDay)
    {
        var next = RecurringInvoiceService.GetNextRunDate(new DateTime(year, month, day), dueDay);

        Assert.Equal(new DateTime(expectedYear, expectedMonth, expectedDay), next);
    }
}
