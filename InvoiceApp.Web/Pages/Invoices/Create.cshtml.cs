using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Pages.Invoices;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;

    public CreateModel(AppDbContext db) => _db = db;

    [BindProperty]
    public Invoice Invoice { get; set; } = new();

    [BindProperty]
    public List<LineItemInput> Items { get; set; } = new();

    public CompanySettings? Company { get; set; }
    public List<SavedRate> SavedRates { get; set; } = new();
    public string NextInvoiceNumber { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        Company = await _db.CompanySettings.FindAsync(1);
        SavedRates = await _db.SavedRates.OrderBy(r => r.Description).ToListAsync();

        var lastInvoice = await _db.Invoices.OrderByDescending(i => i.Id).FirstOrDefaultAsync();
        int nextNum = 1;
        if (lastInvoice != null)
        {
            var parts = lastInvoice.InvoiceNumber.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out int n))
                nextNum = n + 1;
        }
        NextInvoiceNumber = $"INV-{nextNum:D3}";

        Invoice.InvoiceDate = DateTime.Today;
        Invoice.VATRate = 15;
        Items.Add(new LineItemInput());
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Invoice.InvoiceNumber))
            ModelState.AddModelError("Invoice.InvoiceNumber", "Invoice number is required.");

        if (string.IsNullOrWhiteSpace(Invoice.ClientName))
            ModelState.AddModelError("Invoice.ClientName", "Client name is required.");

        var validItems = Items.Where(i => !string.IsNullOrWhiteSpace(i.Description)).ToList();
        if (!validItems.Any())
            ModelState.AddModelError("Items", "At least one line item with a description is required.");

        if (!ModelState.IsValid)
        {
            Company = await _db.CompanySettings.FindAsync(1);
            SavedRates = await _db.SavedRates.OrderBy(r => r.Description).ToListAsync();
            return Page();
        }

        Invoice.LineItems = Items
            .Where(i => !string.IsNullOrWhiteSpace(i.Description))
            .Select((i, idx) => new LineItem
            {
                Description = i.Description,
                Unit = i.Unit,
                Quantity = i.Quantity,
                Rate = i.Rate,
                Amount = i.Quantity * i.Rate,
                SortOrder = idx
            }).ToList();

        Invoice.SubTotal = Invoice.LineItems.Sum(l => l.Amount);

        if (Invoice.VATEnabled)
            Invoice.VATAmount = Math.Round(Invoice.SubTotal * Invoice.VATRate / 100, 2);
        else
            Invoice.VATAmount = 0;

        if (Invoice.RetentionEnabled)
            Invoice.RetentionAmount = Math.Round(Invoice.SubTotal * Invoice.RetentionPercentage / 100, 2);
        else
            Invoice.RetentionAmount = 0;

        Invoice.TotalAmount = Invoice.SubTotal + Invoice.VATAmount;
        if (Invoice.RetentionEnabled && Invoice.RetentionType == "Deposit")
            Invoice.TotalAmount -= Invoice.RetentionAmount;
        else if (Invoice.RetentionEnabled && Invoice.RetentionType == "Holdback")
            Invoice.TotalAmount -= Invoice.RetentionAmount;

        Invoice.CreatedAt = DateTime.Now;

        _db.Invoices.Add(Invoice);
        await _db.SaveChangesAsync();

        return RedirectToPage("/Invoices/Preview", new { id = Invoice.Id });
    }

    public class LineItemInput
    {
        public string Description { get; set; } = string.Empty;
        public string Unit { get; set; } = "m2";
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
    }
}
