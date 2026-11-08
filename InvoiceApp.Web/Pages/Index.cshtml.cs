using InvoiceApp.Infrastructure.Data;
using InvoiceApp.Web.Tenancy;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Pages;

public class IndexModel : PageModel
{
    private readonly CompanyContext _companyContext;
    private readonly AppDbContext _db;

    public IndexModel(CompanyContext companyContext, AppDbContext db)
    {
        _companyContext = companyContext;
        _db = db;
    }

    public string CompanyName { get; private set; } = string.Empty;
    public bool InvoicingEnabled { get; private set; }
    public bool RentTrackingEnabled { get; private set; }

    // Invoicing KPIs
    public int TotalInvoices { get; private set; }
    public decimal RevenueThisMonth { get; private set; }
    public int UnsentInvoices { get; private set; }

    // Rent KPIs (current month)
    public int RentPaidCount { get; private set; }
    public int RentOutstandingCount { get; private set; }
    public decimal RentCollected { get; private set; }
    public decimal RentDue { get; private set; }

    public async Task OnGetAsync()
    {
        var company = await _companyContext.GetAsync();
        if (company == null) return;

        CompanyName = company.Name;
        InvoicingEnabled = company.InvoicingEnabled;
        RentTrackingEnabled = company.RentTrackingEnabled;

        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        if (InvoicingEnabled)
        {
            TotalInvoices = await _db.Invoices.CountAsync();
            RevenueThisMonth = await _db.Invoices
                .Where(i => i.InvoiceDate >= monthStart && i.InvoiceDate < nextMonth)
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;
            UnsentInvoices = await _db.Invoices
                .CountAsync(i => i.ClientEmail != null && i.ClientEmail != "" && i.EmailStatus != "Sent");
        }

        if (RentTrackingEnabled)
        {
            var payments = await _db.RentPayments
                .Where(p => p.Month == now.Month && p.Year == now.Year)
                .ToListAsync();
            RentPaidCount = payments.Count(p => p.IsPaid);
            RentOutstandingCount = payments.Count(p => !p.IsPaid);
            RentDue = payments.Sum(p => p.AmountDue);
            RentCollected = payments.Where(p => p.IsPaid).Sum(p => p.AmountPaid ?? 0);
        }
    }
}
