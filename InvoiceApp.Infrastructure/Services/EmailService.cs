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
