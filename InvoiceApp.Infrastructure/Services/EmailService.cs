using System.Text.RegularExpressions;
using InvoiceApp.Core.Entities;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace InvoiceApp.Infrastructure.Services;

/// <summary>A single outbound email with an attached invoice PDF.</summary>
public record EmailSendRequest(
    string SmtpHost,
    int SmtpPort,
    bool UseSsl,
    string Username,
    string Password,
    string FromName,
    string FromEmail,
    string ToEmail,
    string Subject,
    string Body,
    byte[] PdfBytes,
    string PdfFileName);

public sealed record InvoiceEmailContent(string Subject, string Body, string PdfFileName);

public static class InvoiceEmailComposer
{
    public static InvoiceEmailContent Compose(Invoice invoice, CompanySettings? company, string fromName)
    {
        var companyName = string.IsNullOrWhiteSpace(company?.Name)
            ? (string.IsNullOrWhiteSpace(fromName) ? "Your company" : fromName.Trim())
            : company.Name.Trim();
        var phone = company?.Phone?.Trim();
        var signature = string.IsNullOrWhiteSpace(phone)
            ? $"Kind regards,\n{companyName}"
            : $"Kind regards,\n{companyName}\n{phone}";
        var safeFileInvoice = ToFilePart(invoice.InvoiceNumber, "Invoice");
        var safeFileCompany = ToFilePart(companyName, "Company");

        return new InvoiceEmailContent(
            Subject: $"Invoice {invoice.InvoiceNumber} from {companyName}",
            Body: $"Good day {invoice.ClientName},\n\nPlease find the invoice attached for your review and payment.\n\n{signature}",
            PdfFileName: $"Invoice-{safeFileInvoice}-{safeFileCompany}.pdf");
    }

    private static string ToFilePart(string value, string fallback)
    {
        var part = Regex.Replace(value, "[^A-Za-z0-9]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(part) ? fallback : part;
    }
}

public interface IEmailService
{
    Task SendInvoiceAsync(EmailSendRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Sends invoice emails over SMTP via MailKit. The caller supplies the (already
/// decrypted) SMTP password — this service never touches the encrypted store.
/// </summary>
public class EmailService : IEmailService
{
    public async Task SendInvoiceAsync(EmailSendRequest request, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            string.IsNullOrWhiteSpace(request.FromName) ? request.FromEmail : request.FromName,
            request.FromEmail));
        message.To.Add(MailboxAddress.Parse(request.ToEmail));
        message.Subject = request.Subject;

        var builder = new BodyBuilder { TextBody = request.Body };
        builder.Attachments.Add(request.PdfFileName, request.PdfBytes, ContentType.Parse("application/pdf"));
        message.Body = builder.ToMessageBody();

        // 465 = implicit TLS; 587/other = STARTTLS when SSL is enabled, else plain.
        var socketOptions = request.UseSsl
            ? (request.SmtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
            : SecureSocketOptions.None;

        using var client = new SmtpClient();
        await client.ConnectAsync(request.SmtpHost, request.SmtpPort, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Username))
            await client.AuthenticateAsync(request.Username, request.Password, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
