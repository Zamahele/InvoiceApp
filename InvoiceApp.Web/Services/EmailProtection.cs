namespace InvoiceApp.Web.Pages.Settings;

/// <summary>
/// Shared Data Protection purpose for the SMTP password. The Email settings page
/// encrypts with it and the send-invoice handler decrypts with it — they must match.
/// </summary>
public static class EmailProtection
{
    public const string Purpose = "InvoiceApp.EmailSettings.Password.v1";
}
