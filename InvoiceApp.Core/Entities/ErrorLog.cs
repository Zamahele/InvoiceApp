namespace InvoiceApp.Core.Entities;

public class ErrorLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string RequestPath { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? InnerMessage { get; set; }
}
