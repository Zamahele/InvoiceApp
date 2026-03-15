namespace InvoiceApp.Core.Entities;

public class BankingDetails
{
    public int Id { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string? AccountType { get; set; }
    public string? AccountNumber { get; set; }
    public string? BranchCode { get; set; }
}
