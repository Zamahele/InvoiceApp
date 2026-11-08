using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Infrastructure.Services;

/// <summary>Shared Data Protection purpose for the SMTP password.</summary>
public static class EmailProtection
{
    public const string Purpose = "InvoiceApp.EmailSettings.Password.v1";
}

/// <summary>
/// Builds the invoice PDF, sends it via the configured SMTP settings, and records
/// the delivery outcome (EmailStatus / EmailSentAt / EmailError) on the invoice.
/// Shared by the recurring-invoice worker and the manual send on the Preview page.
/// </summary>
public class InvoiceEmailSender
{
    private readonly AppDbContext _db;
    private readonly InvoicePdfService _pdf;
    private readonly IEmailService _email;
    private readonly IDataProtector _protector;

    public InvoiceEmailSender(
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

    /// <summary>
    /// Sends the invoice by email and persists the outcome. Returns true on success.
    /// Never throws — failures are recorded on the invoice and returned as false.
    /// </summary>
    public async Task<bool> SendAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(invoice.ClientEmail))
            return await RecordFailureAsync(invoice, "No client email on the invoice.", cancellationToken);

        var settings = await _db.EmailSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings == null || string.IsNullOrEmpty(settings.SmtpHost))
            return await RecordFailureAsync(invoice, "Email is not configured.", cancellationToken);

        string password;
        try
        {
            password = _protector.Unprotect(settings.Password);
        }
        catch
        {
            return await RecordFailureAsync(invoice, "Could not read the saved email password.", cancellationToken);
        }

        try
        {
            var company = await _db.CompanySettings.FirstOrDefaultAsync(cancellationToken);
            var banking = await _db.BankingDetails.FirstOrDefaultAsync(cancellationToken);
            var pdf = _pdf.Generate(invoice, company, banking);
            var companyName = company?.Name ?? settings.FromName;
            var fromName = string.IsNullOrWhiteSpace(settings.FromName) ? companyName : settings.FromName;
            var content = InvoiceEmailComposer.Compose(invoice, company, fromName);

            await _email.SendInvoiceAsync(new EmailSendRequest(
                SmtpHost: settings.SmtpHost,
                SmtpPort: settings.SmtpPort,
                UseSsl: settings.UseSsl,
                Username: settings.Username,
                Password: password,
                FromName: fromName,
                FromEmail: settings.FromEmail,
                ToEmail: invoice.ClientEmail!,
                Subject: content.Subject,
                Body: content.Body,
                PdfBytes: pdf,
                PdfFileName: content.PdfFileName,
                CcEmail: settings.CcEmail), cancellationToken);

            invoice.EmailStatus = "Sent";
            invoice.EmailSentAt = DateTime.UtcNow;
            invoice.EmailError = null;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            return await RecordFailureAsync(invoice, exception.Message, cancellationToken);
        }
    }

    private async Task<bool> RecordFailureAsync(Invoice invoice, string error, CancellationToken cancellationToken)
    {
        invoice.EmailStatus = "Failed";
        invoice.EmailError = error.Length > 1000 ? error[..1000] : error;
        await _db.SaveChangesAsync(cancellationToken);
        return false;
    }
}
