using System.Net;
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
        string HtmlBody,
    byte[] PdfBytes,
    string PdfFileName);

public sealed record InvoiceEmailContent(string Subject, string Body, string HtmlBody, string PdfFileName);

public static class InvoiceEmailComposer
{
        public static InvoiceEmailContent Compose(Invoice invoice, CompanySettings? company, string fromName, string fromEmail)
        {
                var companyName = string.IsNullOrWhiteSpace(company?.Name)
                        ? (string.IsNullOrWhiteSpace(fromName) ? "Your company" : fromName.Trim())
                        : company.Name.Trim();
                var safeCompany = WebUtility.HtmlEncode(companyName);
                var safeMark = WebUtility.HtmlEncode(GetCompanyMark(companyName));
                var safeInvoiceNumber = WebUtility.HtmlEncode(invoice.InvoiceNumber);
                var safeClientName = WebUtility.HtmlEncode(invoice.ClientName);
                var invoiceDate = invoice.InvoiceDate.ToString("dd MMM yyyy");
                var total = invoice.TotalAmount.ToString("N2");
                var address = string.Join(", ", new[]
                {
                        company?.AddressLine1,
                        company?.AddressLine2,
                        company?.City,
                        company?.PostalCode
                }.Where(value => !string.IsNullOrWhiteSpace(value)));

                var contactLines = new List<string>();
                if (!string.IsNullOrWhiteSpace(address))
                        contactLines.Add($"<div style=\"margin-top:5px;color:#526276;font-size:14px;line-height:21px;\">{WebUtility.HtmlEncode(address)}</div>");
                if (!string.IsNullOrWhiteSpace(company?.Phone))
                        contactLines.Add($"<div style=\"margin-top:5px;color:#526276;font-size:14px;line-height:21px;\">{WebUtility.HtmlEncode(company.Phone)}</div>");
                if (!string.IsNullOrWhiteSpace(fromEmail))
                        contactLines.Add($"<div style=\"margin-top:5px;font-size:14px;line-height:21px;\"><a href=\"mailto:{WebUtility.HtmlEncode(fromEmail)}\" style=\"color:#0f3460;text-decoration:none;\">{WebUtility.HtmlEncode(fromEmail)}</a></div>");

                var html = $"""
                        <!doctype html>
                        <html lang="en">
                        <body style="margin:0;padding:0;background-color:#f2f4f7;font-family:Arial,Helvetica,sans-serif;color:#243247;">
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f2f4f7;padding:28px 12px;">
                                <tr><td align="center">
                                    <table role="presentation" width="600" cellspacing="0" cellpadding="0" style="width:100%;max-width:600px;background:#ffffff;border:1px solid #e2e7ed;border-radius:8px;overflow:hidden;">
                                        <tr><td style="background:#0f3460;padding:25px 30px;border-bottom:4px solid #c9a84c;">
                                            <table role="presentation" cellspacing="0" cellpadding="0"><tr>
                                                <td align="center" valign="middle" width="42" height="42" style="width:42px;height:42px;background:#c9a84c;border-radius:6px;color:#0f3460;font-size:15px;font-weight:bold;">{safeMark}</td>
                                                <td style="padding-left:12px;color:#f0c96a;font-size:15px;font-weight:bold;">{safeCompany}</td>
                                            </tr></table>
                                            <div style="margin-top:15px;color:#ffffff;font-size:23px;font-weight:bold;">Invoice {safeInvoiceNumber}</div>
                                        </td></tr>
                                        <tr><td style="padding:28px 30px 18px;">
                                            <div style="font-size:16px;line-height:25px;">Hello {safeClientName},</div>
                                            <p style="margin:14px 0 20px;color:#526276;font-size:15px;line-height:24px;">Please find your invoice attached. The invoice summary is below for reference.</p>
                                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border:1px solid #e2e7ed;border-radius:6px;">
                                                <tr><td style="padding:12px 15px;color:#667085;font-size:13px;border-bottom:1px solid #edf0f4;">Invoice number</td><td align="right" style="padding:12px 15px;color:#0a2444;font-size:14px;font-weight:bold;border-bottom:1px solid #edf0f4;">{safeInvoiceNumber}</td></tr>
                                                <tr><td style="padding:12px 15px;color:#667085;font-size:13px;border-bottom:1px solid #edf0f4;">Invoice date</td><td align="right" style="padding:12px 15px;color:#243247;font-size:14px;border-bottom:1px solid #edf0f4;">{WebUtility.HtmlEncode(invoiceDate)}</td></tr>
                                                <tr><td style="padding:14px 15px;color:#243247;font-size:14px;font-weight:bold;">Total due</td><td align="right" style="padding:14px 15px;color:#0f3460;font-size:18px;font-weight:bold;">R {WebUtility.HtmlEncode(total)}</td></tr>
                                            </table>
                                            <div style="margin-top:18px;padding:13px 15px;background:#f4f7fa;border-left:4px solid #c9a84c;color:#526276;font-size:13px;line-height:20px;">The PDF invoice is attached to this email for your records.</div>
                                            <p style="margin:22px 0 0;color:#526276;font-size:14px;line-height:22px;">Kind regards,<br><strong style="color:#0a2444;">{safeCompany}</strong></p>
                                        </td></tr>
                                        <tr><td style="padding:18px 30px 22px;background:#f8fafc;border-top:1px solid #e2e7ed;">
                                            <div style="color:#0a2444;font-size:11px;font-weight:bold;letter-spacing:1.2px;text-transform:uppercase;">Company contact</div>
                                            {string.Join("\n", contactLines)}
                                        </td></tr>
                                    </table>
                                </td></tr>
                            </table>
                        </body>
                        </html>
                        """;

                var safeFileInvoice = ToFilePart(invoice.InvoiceNumber, "Invoice");
                var safeFileCompany = ToFilePart(companyName, "Company");
                return new InvoiceEmailContent(
                        Subject: $"Invoice {invoice.InvoiceNumber} from {companyName}",
                        Body: $"Good day {invoice.ClientName},\n\nPlease find invoice {invoice.InvoiceNumber} attached.\nInvoice date: {invoiceDate}\nTotal due: R {total}\n\nKind regards,\n{companyName}\n{address}\n{company?.Phone}\n{fromEmail}",
                        HtmlBody: html,
                        PdfFileName: $"Invoice-{safeFileInvoice}-{safeFileCompany}.pdf");
        }

        private static string ToFilePart(string value, string fallback)
        {
                var part = Regex.Replace(value, "[^A-Za-z0-9]+", "-").Trim('-');
                return string.IsNullOrWhiteSpace(part) ? fallback : part;
        }

        private static string GetCompanyMark(string companyName)
        {
            var words = Regex.Matches(companyName, "[A-Za-z0-9]+").Select(match => match.Value).ToList();
            if (words.Count == 0) return "CO";
            return string.Concat(words.Take(2).Select(word => char.ToUpperInvariant(word[0])));
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

        var builder = new BodyBuilder { TextBody = request.Body, HtmlBody = request.HtmlBody };
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
