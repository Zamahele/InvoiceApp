namespace InvoiceApp.Core.Entities;

public class RentSettings
{
    public int Id { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string? AgentPhone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
}
