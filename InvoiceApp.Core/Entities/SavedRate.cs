namespace InvoiceApp.Core.Entities;

public class SavedRate : ITenantOwned
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Description { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Rate { get; set; }
}
