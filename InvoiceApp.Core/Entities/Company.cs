namespace InvoiceApp.Core.Entities;

/// <summary>
/// A registered tenant. One login maps to exactly one company; every piece of
/// invoicing and rent data is owned by a company (see <see cref="ITenantOwned"/>).
/// </summary>
public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Service subscriptions — toggleable any time from Settings.
    public bool InvoicingEnabled { get; set; }
    public bool RentTrackingEnabled { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
