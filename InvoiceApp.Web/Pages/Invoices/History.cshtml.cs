using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Pages.Invoices;

public class HistoryModel : PageModel
{
    private readonly AppDbContext _db;

    public HistoryModel(AppDbContext db) => _db = db;

    public List<Invoice> Invoices { get; set; } = new();

    public async Task OnGetAsync()
    {
        Invoices = await _db.Invoices
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var invoice = await _db.Invoices.Include(i => i.LineItems).FirstOrDefaultAsync(i => i.Id == id);
        if (invoice != null)
        {
            _db.Invoices.Remove(invoice);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
