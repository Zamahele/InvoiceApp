using InvoiceApp.Infrastructure.Data;
using InvoiceApp.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Pages.Invoices;

public class DownloadPdfModel : PageModel
{
    private readonly AppDbContext _db;

    public DownloadPdfModel(AppDbContext db) => _db = db;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var invoice = await _db.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return NotFound();

        var company = await _db.CompanySettings.FindAsync(1);
        var banking = await _db.BankingDetails.FindAsync(1);

        var service = new InvoicePdfService();
        var pdf = service.Generate(invoice, company, banking);

        var filename = $"{invoice.InvoiceNumber}.pdf";
        return File(pdf, "application/pdf", filename);
    }
}
