namespace InvoiceApp.Core.Entities;

public class Invoice : ITenantOwned
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public bool IsReissue { get; set; }
    public DateTime? ReissueDate { get; set; }

    // Bill To
    public string ClientName { get; set; } = string.Empty;
    public string? ClientEmail { get; set; }
    public string? ClientAddressLine1 { get; set; }
    public string? ClientAddressLine2 { get; set; }
    public string? ClientCity { get; set; }
    public string? ClientPostalCode { get; set; }

    // Financial data
    public decimal SubTotal { get; set; }
    public bool VATEnabled { get; set; }
    public decimal VATRate { get; set; } = 15;
    public decimal VATAmount { get; set; }
    public bool RetentionEnabled { get; set; }
    public string RetentionType { get; set; } = "Deposit"; // Deposit or Holdback
    public decimal RetentionPercentage { get; set; }
    public decimal RetentionAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // New: Invoice Category (e.g., Maintenance, Hosting, Development)
    public string Category { get; set; } = "General";

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<LineItem> LineItems { get; set; } = new();
}