namespace InvoiceApp.Core.Entities;

public class RecurringInvoiceLineItem
{
    public int Id { get; set; }
    public int RecurringInvoiceScheduleId { get; set; }
    public RecurringInvoiceSchedule Schedule { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public string Unit { get; set; } = "unit";
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public int SortOrder { get; set; }
}