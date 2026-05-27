using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using InvoiceApp.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InvoiceApp.Tests.Data;

public class TenancyTests
{
    private sealed class StubCompany : ICurrentCompanyProvider
    {
        public int? CompanyId { get; init; }
    }

    private static DbContextOptions<AppDbContext> SharedOptions(string name) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

    [Fact]
    public async Task Insert_Is_Stamped_With_Current_Company()
    {
        var options = SharedOptions(Guid.NewGuid().ToString());
        using var db = new AppDbContext(options, new StubCompany { CompanyId = 7 });

        db.Invoices.Add(new Invoice { InvoiceNumber = "INV-001", InvoiceDate = DateTime.Today, ClientName = "Acme" });
        await db.SaveChangesAsync();

        var saved = await db.Invoices.SingleAsync();
        Assert.Equal(7, saved.CompanyId);
    }

    [Fact]
    public async Task Queries_Are_Isolated_Between_Companies()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var companyA = new AppDbContext(SharedOptions(dbName), new StubCompany { CompanyId = 1 }))
        {
            companyA.Invoices.Add(new Invoice { InvoiceNumber = "A-001", InvoiceDate = DateTime.Today, ClientName = "A" });
            await companyA.SaveChangesAsync();
        }

        using (var companyB = new AppDbContext(SharedOptions(dbName), new StubCompany { CompanyId = 2 }))
        {
            companyB.Invoices.Add(new Invoice { InvoiceNumber = "B-001", InvoiceDate = DateTime.Today, ClientName = "B" });
            await companyB.SaveChangesAsync();

            // Company B sees only its own data.
            var visible = await companyB.Invoices.ToListAsync();
            Assert.Single(visible);
            Assert.Equal("B-001", visible[0].InvoiceNumber);

            // ...and cannot reach company A's row by id.
            var leaked = await companyB.Invoices.FirstOrDefaultAsync(i => i.ClientName == "A");
            Assert.Null(leaked);
        }
    }

    [Fact]
    public async Task Null_Company_Bypasses_Filter()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var seed = new AppDbContext(SharedOptions(dbName), new StubCompany { CompanyId = 1 }))
        {
            seed.Invoices.Add(new Invoice { InvoiceNumber = "X", InvoiceDate = DateTime.Today, ClientName = "X" });
            await seed.SaveChangesAsync();
        }
        using (var seed2 = new AppDbContext(SharedOptions(dbName), new StubCompany { CompanyId = 2 }))
        {
            seed2.Invoices.Add(new Invoice { InvoiceNumber = "Y", InvoiceDate = DateTime.Today, ClientName = "Y" });
            await seed2.SaveChangesAsync();
        }

        // No current company (migrations/tests) -> see everything.
        using var unscoped = new AppDbContext(SharedOptions(dbName));
        Assert.Equal(2, await unscoped.Invoices.CountAsync());
    }
}
