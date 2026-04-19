namespace InvoiceApp.Core.Entities;

public class Property
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string? AgentPhone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
