namespace InvoiceApp.Core.Entities;

public class SavedRate
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Rate { get; set; }
}
