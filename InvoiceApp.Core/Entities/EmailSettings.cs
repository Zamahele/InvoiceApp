namespace InvoiceApp.Core.Entities;

/// <summary>
/// Per-tenant SMTP configuration used to email invoice PDFs. One row per company.
/// <para><see cref="Password"/> is stored encrypted at rest (ASP.NET Data Protection);
/// it is only decrypted in-memory when an email is actually sent.</para>
/// </summary>
public class EmailSettings : ITenantOwned
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;

    public string Username { get; set; } = string.Empty;

    /// <summary>Encrypted SMTP password. Never store or display the plaintext.</summary>
    public string Password { get; set; } = string.Empty;

    public string FromName { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
}
