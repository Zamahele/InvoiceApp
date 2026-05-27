using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using InvoiceApp.Infrastructure.Services;
using InvoiceApp.Web.Pages.Settings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Pages.Invoices;

public class PreviewModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly InvoicePdfService _pdf;
    private readonly IEmailService _email;
    private readonly IDataProtector _protector;

    public PreviewModel(
        AppDbContext db,
        InvoicePdfService pdf,
        IEmailService email,
        IDataProtectionProvider dataProtection)
    {
        _db = db;
        _pdf = pdf;
        _email = email;
        _protector = dataProtection.CreateProtector(EmailProtection.Purpose);
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

        if (string.IsNullOrWhiteSpace(invoice.ClientEmail))
            return Fail(id, "This invoice has no client email. Edit the invoice to add one.");

        var settings = await _db.EmailSettings.FirstOrDefaultAsync();
        if (settings == null || string.IsNullOrEmpty(settings.SmtpHost))
            return Fail(id, "Email is not configured. Set it up under Settings → Email.");

        var company = await _db.CompanySettings.FirstOrDefaultAsync();
        var banking = await _db.BankingDetails.FirstOrDefaultAsync();

        string password;
        try
        {
            password = _protector.Unprotect(settings.Password);
        }
        catch
        {
            return Fail(id, "Could not read the saved email password. Re-enter it under Settings → Email.");
        }

        try
        {
            var pdf = _pdf.Generate(invoice, company, banking);
            var companyName = company?.Name ?? settings.FromName;

            await _email.SendInvoiceAsync(new EmailSendRequest(
                SmtpHost: settings.SmtpHost,
                SmtpPort: settings.SmtpPort,
                UseSsl: settings.UseSsl,
                Username: settings.Username,
                Password: password,
                FromName: string.IsNullOrWhiteSpace(settings.FromName) ? companyName : settings.FromName,
                FromEmail: settings.FromEmail,
                ToEmail: invoice.ClientEmail!,
                Subject: $"Invoice {invoice.InvoiceNumber} from {companyName}",
                Body: $"Good day,\n\nPlease find attached invoice {invoice.InvoiceNumber}.\n\nKind regards,\n{companyName}",
                PdfBytes: pdf,
                PdfFileName: $"{invoice.InvoiceNumber}.pdf"));

            StatusMessage = $"Invoice emailed to {invoice.ClientEmail}.";
            StatusIsError = false;
        }
        catch (Exception ex)
        {
            return Fail(id, $"Sending failed: {ex.Message}");
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
