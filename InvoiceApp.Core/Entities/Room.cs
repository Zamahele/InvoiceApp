namespace InvoiceApp.Core.Entities;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string TenantPhone { get; set; } = string.Empty;
    public decimal RentAmount { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<RentPayment> Payments { get; set; } = new List<RentPayment>();
}
