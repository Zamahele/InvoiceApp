using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using InvoiceApp.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Pages.Invoices;

public class PreviewModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly InvoiceEmailSender _emailSender;

    public PreviewModel(AppDbContext db, InvoiceEmailSender emailSender)
    {
        _db = db;
        _emailSender = emailSender;
    }

    public Invoice? Invoice { get; set; }
    public CompanySettings? Company { get; set; }
    public BankingDetails? Banking { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool StatusIsError { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Invoice = await _db.Invoices
            .Include(i => i.LineItems.OrderBy(l => l.SortOrder))
            .FirstOrDefaultAsync(i => i.Id == id);

        if (Invoice == null) return NotFound();

        Company = await _db.CompanySettings.FirstOrDefaultAsync();
        Banking = await _db.BankingDetails.FirstOrDefaultAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostEmailAsync(int id)
    {
        var invoice = await _db.Invoices
            .Include(i => i.LineItems.OrderBy(l => l.SortOrder))
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return NotFound();

        var sent = await _emailSender.SendAsync(invoice);
        if (sent)
        {
            StatusMessage = $"Invoice emailed to {invoice.ClientEmail}.";
            StatusIsError = false;
        }
        else
        {
            return Fail(id, $"Sending failed: {invoice.EmailError}");
        }

        return RedirectToPage(new { id });
    }

    private IActionResult Fail(int id, string message)
    {
        StatusMessage = message;
        StatusIsError = true;
        return RedirectToPage(new { id });
    }
}
