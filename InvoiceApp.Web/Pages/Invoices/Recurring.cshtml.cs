using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using InvoiceApp.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Pages.Invoices;

public class RecurringModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly RecurringInvoiceService _recurringInvoices;

    public RecurringModel(AppDbContext db, RecurringInvoiceService recurringInvoices)
    {
        _db = db;
        _recurringInvoices = recurringInvoices;
    }

    [BindProperty]
    public int DueDay { get; set; } = 1;

    public Invoice? SourceInvoice { get; private set; }
    public RecurringInvoiceSchedule? ExistingSchedule { get; private set; }
    public List<RecurringInvoiceSchedule> Schedules { get; private set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int? sourceId)
    {
        await _recurringInvoices.GenerateDueInvoicesAsync();
        await LoadSchedulesAsync();

        if (!sourceId.HasValue)
            return Page();

        SourceInvoice = await _db.Invoices
            .Include(invoice => invoice.LineItems.OrderBy(item => item.SortOrder))
            .FirstOrDefaultAsync(invoice => invoice.Id == sourceId.Value);
        if (SourceInvoice == null)
            return NotFound();

        DueDay = SourceInvoice.InvoiceDate.Day;
        ExistingSchedule = await _db.RecurringInvoiceSchedules
            .Include(schedule => schedule.LineItems.OrderBy(item => item.SortOrder))
            .FirstOrDefaultAsync(schedule => schedule.SourceInvoiceId == SourceInvoice.Id);

        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync(int sourceId)
    {
        if (DueDay < 1 || DueDay > 31)
            ModelState.AddModelError(nameof(DueDay), "Choose a day between 1 and 31.");

        var sourceInvoice = await _db.Invoices
            .Include(invoice => invoice.LineItems.OrderBy(item => item.SortOrder))
            .FirstOrDefaultAsync(invoice => invoice.Id == sourceId);
        if (sourceInvoice == null)
            return NotFound();

        if (await _db.RecurringInvoiceSchedules.AnyAsync(schedule => schedule.SourceInvoiceId == sourceId))
        {
            StatusMessage = "This invoice already has a recurring schedule. Use the schedule list to resume it if it is paused.";
            return RedirectToPage(new { sourceId });
        }

        if (!ModelState.IsValid)
        {
            SourceInvoice = sourceInvoice;
            await LoadSchedulesAsync();
            return Page();
        }

        var today = DateTime.Today;
        var nextMonth = new DateTime(today.Year, today.Month, 1).AddMonths(1);
        var firstRunDate = new DateTime(
            nextMonth.Year,
            nextMonth.Month,
            Math.Min(DueDay, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month)));

        var schedule = new RecurringInvoiceSchedule
        {
            CompanyId = sourceInvoice.CompanyId,
            SourceInvoiceId = sourceInvoice.Id,
            ClientName = sourceInvoice.ClientName,
            ClientEmail = sourceInvoice.ClientEmail,
            ClientAddressLine1 = sourceInvoice.ClientAddressLine1,
            ClientAddressLine2 = sourceInvoice.ClientAddressLine2,
            ClientCity = sourceInvoice.ClientCity,
            ClientPostalCode = sourceInvoice.ClientPostalCode,
            Category = sourceInvoice.Category,
            Notes = sourceInvoice.Notes,
            VATEnabled = sourceInvoice.VATEnabled,
            VATRate = sourceInvoice.VATRate,
            RetentionEnabled = sourceInvoice.RetentionEnabled,
            RetentionType = sourceInvoice.RetentionType,
            RetentionPercentage = sourceInvoice.RetentionPercentage,
            DueDay = DueDay,
            NextRunDate = firstRunDate,
            IsActive = true,
            LineItems = sourceInvoice.LineItems
                .OrderBy(item => item.SortOrder)
                .Select(item => new RecurringInvoiceLineItem
                {
                    Description = item.Description,
                    Unit = item.Unit,
                    Quantity = item.Quantity,
                    Rate = item.Rate,
                    SortOrder = item.SortOrder
                }).ToList()
        };

        _db.RecurringInvoiceSchedules.Add(schedule);
        await _db.SaveChangesAsync();
        StatusMessage = $"Monthly invoice scheduled for day {DueDay}. The first invoice is due {firstRunDate:dd MMM yyyy}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var schedule = await _db.RecurringInvoiceSchedules.FirstOrDefaultAsync(item => item.Id == id);
        if (schedule == null)
            return NotFound();

        schedule.IsActive = !schedule.IsActive;
        await _db.SaveChangesAsync();
        StatusMessage = schedule.IsActive ? "Recurring invoice resumed." : "Recurring invoice paused.";
        return RedirectToPage();
    }

    private async Task LoadSchedulesAsync()
    {
        Schedules = await _db.RecurringInvoiceSchedules
            .Include(schedule => schedule.LineItems.OrderBy(item => item.SortOrder))
            .OrderBy(schedule => schedule.ClientName)
            .ToListAsync();
    }
}
