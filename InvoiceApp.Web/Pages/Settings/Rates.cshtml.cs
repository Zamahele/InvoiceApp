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

    // Autocomplete hints for the Unit field — existing units plus common defaults.
    // The field is free-text, so any new unit can still be entered.
    public List<string> UnitSuggestions { get; set; } = new();

    [BindProperty]
    public SavedRate NewRate { get; set; } = new();

    public async Task OnGetAsync()
    {
        Rates = await _db.SavedRates.OrderBy(r => r.Description).ToListAsync();
        UnitSuggestions = BuildUnitSuggestions(Rates);
    }

    private static List<string> BuildUnitSuggestions(IEnumerable<SavedRate> rates) =>
        new[] { "monthly", "hourly" }
            .Concat(rates.Select(r => r.Unit))
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(u => u, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public async Task<IActionResult> OnPostAddAsync()
    {
        NewRate.Description = NewRate.Description?.Trim() ?? string.Empty;
        NewRate.Unit = NewRate.Unit?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(NewRate.Description))
            ModelState.AddModelError("NewRate.Description", "Rate description is required.");

        if (string.IsNullOrWhiteSpace(NewRate.Unit))
            ModelState.AddModelError("NewRate.Unit", "Rate unit is required.");

        if (NewRate.Rate < 0)
            ModelState.AddModelError("NewRate.Rate", "Rate cannot be negative.");

        if (!ModelState.IsValid)
        {
            Rates = await _db.SavedRates.OrderBy(r => r.Description).ToListAsync();
            UnitSuggestions = BuildUnitSuggestions(Rates);
            return Page();
        }

        _db.SavedRates.Add(NewRate);
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var rate = await _db.SavedRates.FirstOrDefaultAsync(r => r.Id == id);
        if (rate != null)
        {
            _db.SavedRates.Remove(rate);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
