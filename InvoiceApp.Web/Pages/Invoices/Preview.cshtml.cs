using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Pages.Invoices;

public class PreviewModel : PageModel
{
    private readonly AppDbContext _db;

    public PreviewModel(AppDbContext db) => _db = db;

    public Invoice? Invoice { get; set; }
    public CompanySettings? Company { get; set; }
    public BankingDetails? Banking { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Invoice = await _db.Invoices
            .Include(i => i.LineItems.OrderBy(l => l.SortOrder))
            .FirstOrDefaultAsync(i => i.Id == id);

        if (Invoice == null) return NotFound();

        Company = await _db.CompanySettings.FindAsync(1);
        Banking = await _db.BankingDetails.FindAsync(1);

        return Page();
    }
}
