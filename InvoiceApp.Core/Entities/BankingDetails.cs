namespace InvoiceApp.Core.Entities;

public class BankingDetails : ITenantOwned
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public string BankName { get; set; } = string.Empty;
    public string? AccountType { get; set; }
    public string? AccountNumber { get; set; }
    public string? BranchCode { get; set; }
}
