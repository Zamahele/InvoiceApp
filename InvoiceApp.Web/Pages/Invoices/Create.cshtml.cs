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
        Invoice.InvoiceNumber = Invoice.InvoiceNumber?.Trim() ?? string.Empty;
        Invoice.ClientName = Invoice.ClientName?.Trim() ?? string.Empty;

        var validItems = ValidateInvoiceInput();

        if (!ModelState.IsValid)
        {
            Company = await _db.CompanySettings.FindAsync(1);
            SavedRates = await _db.SavedRates.OrderBy(r => r.Description).ToListAsync();
            NextInvoiceNumber = Invoice.InvoiceNumber;
            return Page();
        }

        Invoice.LineItems = Items
            .Where(i => !string.IsNullOrWhiteSpace(i.Description))
            .Select((i, idx) => new LineItem
            {
                Description = i.Description.Trim(),
                Unit = string.IsNullOrWhiteSpace(i.Unit) ? "unit" : i.Unit.Trim(),
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

    private List<LineItemInput> ValidateInvoiceInput()
    {
        if (string.IsNullOrWhiteSpace(Invoice.InvoiceNumber))
            ModelState.AddModelError("Invoice.InvoiceNumber", "Invoice number is required.");

        if (string.IsNullOrWhiteSpace(Invoice.ClientName))
            ModelState.AddModelError("Invoice.ClientName", "Client name is required.");

        if (Invoice.InvoiceDate == default)
            ModelState.AddModelError("Invoice.InvoiceDate", "Invoice date is required.");

        if (Invoice.IsReissue && !Invoice.ReissueDate.HasValue)
            ModelState.AddModelError("Invoice.ReissueDate", "Re-issue date is required when re-issue is enabled.");

        if (Invoice.VATEnabled && (Invoice.VATRate < 0 || Invoice.VATRate > 100))
            ModelState.AddModelError("Invoice.VATRate", "VAT rate must be between 0 and 100.");

        if (Invoice.RetentionEnabled)
        {
            if (Invoice.RetentionPercentage < 0 || Invoice.RetentionPercentage > 100)
                ModelState.AddModelError("Invoice.RetentionPercentage", "Retention percentage must be between 0 and 100.");

            if (Invoice.RetentionType != "Deposit" && Invoice.RetentionType != "Holdback")
                ModelState.AddModelError("Invoice.RetentionType", "Retention type must be Deposit or Holdback.");
        }

        var validItems = new List<LineItemInput>();
        var itemsToValidate = Items ?? new List<LineItemInput>();

        for (var index = 0; index < itemsToValidate.Count; index++)
        {
            var item = itemsToValidate[index];
            var hasAnyValue = !string.IsNullOrWhiteSpace(item.Description)
                || item.Quantity != 0
                || item.Rate != 0;

            if (!hasAnyValue)
                continue;

            if (string.IsNullOrWhiteSpace(item.Description))
                ModelState.AddModelError($"Items[{index}].Description", "Description is required.");

            if (item.Quantity <= 0)
                ModelState.AddModelError($"Items[{index}].Quantity", "Quantity must be greater than 0.");

            if (item.Rate < 0)
                ModelState.AddModelError($"Items[{index}].Rate", "Rate cannot be negative.");

            if (!string.IsNullOrWhiteSpace(item.Description) && item.Quantity > 0 && item.Rate >= 0)
                validItems.Add(item);
        }

        if (!validItems.Any())
            ModelState.AddModelError("Items", "At least one valid line item is required.");

        return validItems;
    }

    public class LineItemInput
    {
        public string Description { get; set; } = string.Empty;
        public string Unit { get; set; } = "m2";
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
    }
}
