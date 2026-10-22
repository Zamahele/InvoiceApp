namespace InvoiceApp.Core.Entities;

public class RecurringInvoiceSchedule : ITenantOwned
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
    public int SourceInvoiceId { get; set; }

    public string ClientName { get; set; } = string.Empty;
    public string? ClientEmail { get; set; }
    public string? ClientAddressLine1 { get; set; }
    public string? ClientAddressLine2 { get; set; }
    public string? ClientCity { get; set; }
    public string? ClientPostalCode { get; set; }
    public string Category { get; set; } = "General";
    public string? Notes { get; set; }

    public bool VATEnabled { get; set; }
    public decimal VATRate { get; set; } = 15;
    public bool RetentionEnabled { get; set; }
    public string RetentionType { get; set; } = "Deposit";
    public decimal RetentionPercentage { get; set; }

    public int DueDay { get; set; } = 1;
    public DateTime NextRunDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<RecurringInvoiceLineItem> LineItems { get; set; } = new();
}