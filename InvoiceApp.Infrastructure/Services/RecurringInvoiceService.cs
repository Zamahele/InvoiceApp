using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Infrastructure.Services;

public class RecurringInvoiceService
{
    private readonly AppDbContext _db;

    public RecurringInvoiceService(AppDbContext db) => _db = db;

    public async Task<int> GenerateDueInvoicesAsync(DateTime? today = null, CancellationToken cancellationToken = default)
    {
        var runDate = (today ?? DateTime.Today).Date;
        var schedules = await _db.RecurringInvoiceSchedules
            .Include(schedule => schedule.LineItems.OrderBy(item => item.SortOrder))
            .Where(schedule => schedule.IsActive && schedule.NextRunDate <= runDate)
            .OrderBy(schedule => schedule.NextRunDate)
            .ToListAsync(cancellationToken);

        var generatedCount = 0;
        foreach (var schedule in schedules)
        {
            while (schedule.IsActive && schedule.NextRunDate.Date <= runDate)
            {
                var scheduledDate = schedule.NextRunDate.Date;
                var period = new DateTime(scheduledDate.Year, scheduledDate.Month, 1);
                var alreadyGenerated = await _db.Invoices.AnyAsync(
                    invoice => invoice.RecurringScheduleId == schedule.Id && invoice.RecurringPeriod == period,
                    cancellationToken);

                if (!alreadyGenerated)
                {
                    var invoice = await CreateInvoiceAsync(schedule, scheduledDate, cancellationToken);
                    _db.Invoices.Add(invoice);
                    await _db.SaveChangesAsync(cancellationToken);
                    generatedCount++;
                }

                schedule.NextRunDate = GetNextRunDate(scheduledDate, schedule.DueDay);
            }
        }

        if (schedules.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return generatedCount;
    }

    public static DateTime GetNextRunDate(DateTime currentRunDate, int dueDay)
    {
        var nextMonth = currentRunDate.AddMonths(1);
        var day = Math.Clamp(dueDay, 1, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
        return new DateTime(nextMonth.Year, nextMonth.Month, day);
    }

    private async Task<Invoice> CreateInvoiceAsync(RecurringInvoiceSchedule schedule, DateTime invoiceDate, CancellationToken cancellationToken)
    {
        var lines = schedule.LineItems
            .OrderBy(item => item.SortOrder)
            .Select(item => new LineItem
            {
                Description = item.Description,
                Unit = item.Unit,
                Quantity = item.Quantity,
                Rate = item.Rate,
                Amount = Math.Round(item.Quantity * item.Rate, 2),
                SortOrder = item.SortOrder
            }).ToList();

        var subtotal = lines.Sum(item => item.Amount);
        var vatAmount = schedule.VATEnabled
            ? Math.Round(subtotal * schedule.VATRate / 100, 2)
            : 0;
        var retentionAmount = schedule.RetentionEnabled
            ? Math.Round(subtotal * schedule.RetentionPercentage / 100, 2)
            : 0;
        var total = subtotal + vatAmount - (schedule.RetentionEnabled ? retentionAmount : 0);

        return new Invoice
        {
            CompanyId = schedule.CompanyId,
            RecurringScheduleId = schedule.Id,
            RecurringPeriod = new DateTime(invoiceDate.Year, invoiceDate.Month, 1),
            InvoiceNumber = await GetNextInvoiceNumberAsync(schedule.CompanyId, cancellationToken),
            InvoiceDate = invoiceDate,
            ClientName = schedule.ClientName,
            ClientEmail = schedule.ClientEmail,
            ClientAddressLine1 = schedule.ClientAddressLine1,
            ClientAddressLine2 = schedule.ClientAddressLine2,
            ClientCity = schedule.ClientCity,
            ClientPostalCode = schedule.ClientPostalCode,
            Category = schedule.Category,
            Notes = schedule.Notes,
            VATEnabled = schedule.VATEnabled,
            VATRate = schedule.VATRate,
            VATAmount = vatAmount,
            RetentionEnabled = schedule.RetentionEnabled,
            RetentionType = schedule.RetentionType,
            RetentionPercentage = schedule.RetentionPercentage,
            RetentionAmount = retentionAmount,
            SubTotal = subtotal,
            TotalAmount = total,
            CreatedAt = DateTime.Now,
            LineItems = lines
        };
    }

    private async Task<string> GetNextInvoiceNumberAsync(int companyId, CancellationToken cancellationToken)
    {
        var lastInvoice = await _db.Invoices
            .Where(invoice => invoice.CompanyId == companyId)
            .OrderByDescending(invoice => invoice.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var nextNumber = 1;
        if (lastInvoice != null)
        {
            var parts = lastInvoice.InvoiceNumber.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out var currentNumber))
                nextNumber = currentNumber + 1;
        }
        return $"INV-{nextNumber:D3}";
    }
}
