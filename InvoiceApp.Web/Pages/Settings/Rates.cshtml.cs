using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Pages.Settings;

public class RatesModel : PageModel
{
    private readonly AppDbContext _db;

    public RatesModel(AppDbContext db) => _db = db;

    public List<SavedRate> Rates { get; set; } = new();

    [BindProperty]
    public SavedRate NewRate { get; set; } = new();

    public async Task OnGetAsync()
    {
        Rates = await _db.SavedRates.OrderBy(r => r.Description).ToListAsync();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!string.IsNullOrWhiteSpace(NewRate.Description))
        {
            _db.SavedRates.Add(NewRate);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var rate = await _db.SavedRates.FindAsync(id);
        if (rate != null)
        {
            _db.SavedRates.Remove(rate);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
