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
    public bool IsEditing { get; private set; }

    // Unit options are derived from the rate library (SavedRates) rather than hard-coded,
    // so adding a saved rate with a new unit makes it available with no code change.
    public List<string> Units { get; set; } = new();

    private static List<string> BuildUnits(IEnumerable<SavedRate> rates)
    {
        var units = rates
            .Select(r => r.Unit?.Trim())
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(u => u, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Fallback only when the library is empty, so the dropdown is never blank.
        if (units.Count == 0)
            units.AddRange(new[] { "monthly", "hourly" });

        return units;
    }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        await LoadFormOptionsAsync();

        if (id.HasValue)
        {
            var existingInvoice = await _db.Invoices
                .Include(i => i.LineItems.OrderBy(item => item.SortOrder))
                .FirstOrDefaultAsync(i => i.Id == id.Value);

            if (existingInvoice == null) return NotFound();

            Invoice = existingInvoice;
            Items = existingInvoice.LineItems.Select(item => new LineItemInput
            {
                Description = item.Description,
                Unit = item.Unit,
                Quantity = item.Quantity,
                Rate = item.Rate
            }).ToList();
            NextInvoiceNumber = existingInvoice.InvoiceNumber;
            IsEditing = true;
            return Page();
        }

        Invoice.InvoiceDate = DateTime.Today;
        Invoice.VATRate = 15;
        Items.Add(new LineItemInput());
        return Page();
    }

    private async Task LoadFormOptionsAsync()
    {
        Company = await _db.CompanySettings.FirstOrDefaultAsync();
        SavedRates = await _db.SavedRates.OrderBy(r => r.Description).ToListAsync();
        Units = BuildUnits(SavedRates);

        var lastInvoice = await _db.Invoices.OrderByDescending(i => i.Id).FirstOrDefaultAsync();
        int nextNum = 1;
        if (lastInvoice != null)
        {
            var parts = lastInvoice.InvoiceNumber.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out int n))
                nextNum = n + 1;
        }
        NextInvoiceNumber = $"INV-{nextNum:D3}";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Invoice.InvoiceNumber = Invoice.InvoiceNumber?.Trim() ?? string.Empty;
        Invoice.ClientName = Invoice.ClientName?.Trim() ?? string.Empty;
        Invoice.ClientEmail = Invoice.ClientEmail?.Trim();

        var validItems = ValidateInvoiceInput();

        if (!ModelState.IsValid)
        {
            await LoadFormOptionsAsync();
            NextInvoiceNumber = Invoice.InvoiceNumber;
            IsEditing = Invoice.Id > 0;
            return Page();
        }

        var invoiceToSave = Invoice;
        if (Invoice.Id > 0)
        {
            var existingInvoice = await _db.Invoices
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.Id == Invoice.Id);

            if (existingInvoice == null) return NotFound();

            existingInvoice.InvoiceNumber = Invoice.InvoiceNumber;
            existingInvoice.InvoiceDate = Invoice.InvoiceDate;
            existingInvoice.IsReissue = Invoice.IsReissue;
            existingInvoice.ReissueDate = Invoice.ReissueDate;
            existingInvoice.ClientName = Invoice.ClientName;
            existingInvoice.ClientEmail = Invoice.ClientEmail;
            existingInvoice.ClientAddressLine1 = Invoice.ClientAddressLine1;
            existingInvoice.ClientAddressLine2 = Invoice.ClientAddressLine2;
            existingInvoice.ClientCity = Invoice.ClientCity;
            existingInvoice.ClientPostalCode = Invoice.ClientPostalCode;
            existingInvoice.Category = Invoice.Category;
            existingInvoice.Notes = Invoice.Notes;
            _db.LineItems.RemoveRange(existingInvoice.LineItems);
            existingInvoice.LineItems = new List<LineItem>();
            invoiceToSave = existingInvoice;
            IsEditing = true;
        }

        invoiceToSave.LineItems = validItems
            .Select((i, idx) => new LineItem
            {
                Description = i.Description.Trim(),
                Unit = string.IsNullOrWhiteSpace(i.Unit) ? "unit" : i.Unit.Trim(),
            Quantity = i.Quantity!.Value,
            Rate = i.Rate!.Value,
            Amount = i.Quantity.Value * i.Rate.Value,
                SortOrder = idx
            }).ToList();

        invoiceToSave.SubTotal = invoiceToSave.LineItems.Sum(l => l.Amount);

        if (invoiceToSave.VATEnabled)
            invoiceToSave.VATAmount = Math.Round(invoiceToSave.SubTotal * invoiceToSave.VATRate / 100, 2);
        else
            invoiceToSave.VATAmount = 0;

        if (invoiceToSave.RetentionEnabled)
            invoiceToSave.RetentionAmount = Math.Round(invoiceToSave.SubTotal * invoiceToSave.RetentionPercentage / 100, 2);
        else
            invoiceToSave.RetentionAmount = 0;

        invoiceToSave.TotalAmount = invoiceToSave.SubTotal + invoiceToSave.VATAmount;
        if (invoiceToSave.RetentionEnabled && invoiceToSave.RetentionType == "Deposit")
            invoiceToSave.TotalAmount -= invoiceToSave.RetentionAmount;
        else if (invoiceToSave.RetentionEnabled && invoiceToSave.RetentionType == "Holdback")
            invoiceToSave.TotalAmount -= invoiceToSave.RetentionAmount;

        if (invoiceToSave.Id == 0)
            invoiceToSave.CreatedAt = DateTime.Now;

        if (invoiceToSave.Id == 0)
            _db.Invoices.Add(invoiceToSave);
        await _db.SaveChangesAsync();

        return RedirectToPage("/Invoices/Preview", new { id = invoiceToSave.Id });
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
                || item.Quantity.GetValueOrDefault() != 0
                || item.Rate.GetValueOrDefault() != 0;

            if (!hasAnyValue)
                continue;

            if (string.IsNullOrWhiteSpace(item.Description))
                ModelState.AddModelError($"Items[{index}].Description", "Description is required.");

            if (!item.Quantity.HasValue || item.Quantity <= 0)
                ModelState.AddModelError($"Items[{index}].Quantity", "Quantity must be greater than 0.");

            if (!item.Rate.HasValue || item.Rate < 0)
                ModelState.AddModelError($"Items[{index}].Rate", "Rate is required and cannot be negative.");

            if (!string.IsNullOrWhiteSpace(item.Description)
                && item.Quantity > 0
                && item.Rate >= 0)
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
        public decimal? Quantity { get; set; }
        public decimal? Rate { get; set; }
    }
}
