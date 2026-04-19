using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Pages.Rent;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)] public int Month { get; set; } = DateTime.Today.Month;
    [BindProperty(SupportsGet = true)] public int Year { get; set; } = DateTime.Today.Year;

    public List<RentPayment> Payments { get; set; } = new();
    public int TotalRooms { get; set; }
    public int PaidCount { get; set; }
    public int OutstandingCount { get; set; }
    public decimal TotalDue { get; set; }
    public decimal TotalCollected { get; set; }
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        NormalizePeriod();
        await EnsurePaymentsForMonth();
        await LoadPayments();
    }

    public async Task<IActionResult> OnPostMarkPaidAsync(int id, decimal amountPaid, string? notes)
    {
        NormalizePeriod();
        var payment = await _db.RentPayments.FindAsync(id);
        if (payment == null)
        {
            ModelState.AddModelError(string.Empty, "Rent payment record not found.");
        }
        else
        {
            if (amountPaid < 0)
                ModelState.AddModelError("amountPaid", "Amount paid cannot be negative.");

            if (amountPaid > payment.AmountDue)
                ModelState.AddModelError("amountPaid", "Amount paid cannot be greater than amount due.");

            if (!string.IsNullOrWhiteSpace(notes) && notes.Length > 500)
                ModelState.AddModelError("notes", "Notes cannot exceed 500 characters.");

            if (ModelState.IsValid)
            {
                payment.IsPaid = true;
                payment.AmountPaid = Math.Round(amountPaid, 2);
                payment.PaidDate = DateTime.Today;
                payment.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
                await _db.SaveChangesAsync();
                return RedirectToPage(new { Month, Year });
            }
        }

        await EnsurePaymentsForMonth();
        await LoadPayments();
        return Page();
    }

    public async Task<IActionResult> OnPostUndoAsync(int id)
    {
        NormalizePeriod();
        var payment = await _db.RentPayments.FindAsync(id);
        if (payment != null)
        {
            payment.IsPaid = false;
            payment.AmountPaid = null;
            payment.PaidDate = null;
            payment.Notes = null;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { Month, Year });
    }

    private void NormalizePeriod()
    {
        if (Month < 1 || Month > 12)
            Month = DateTime.Today.Month;

        if (Year < 2000 || Year > 2100)
            Year = DateTime.Today.Year;
    }

    private async Task EnsurePaymentsForMonth()
    {
        var activeRooms = await _db.Rooms.Where(r => r.IsActive).ToListAsync();
        var existingRoomIds = await _db.RentPayments
            .Where(p => p.Month == Month && p.Year == Year)
            .Select(p => p.RoomId)
            .ToListAsync();

        var missing = activeRooms.Where(r => !existingRoomIds.Contains(r.Id)).ToList();
        if (missing.Any())
        {
            foreach (var room in missing)
            {
                _db.RentPayments.Add(new RentPayment
                {
                    RoomId = room.Id,
                    Month = Month,
                    Year = Year,
                    AmountDue = room.RentAmount,
                    IsPaid = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
        }
    }

    private async Task LoadPayments()
    {
        Payments = await _db.RentPayments
            .Include(p => p.Room)
            .Where(p => p.Month == Month && p.Year == Year)
            .OrderBy(p => p.Room.Name)
            .ToListAsync();

        TotalRooms = Payments.Count;
        PaidCount = Payments.Count(p => p.IsPaid);
        OutstandingCount = Payments.Count(p => !p.IsPaid);
        TotalDue = Payments.Sum(p => p.AmountDue);
        TotalCollected = Payments.Where(p => p.IsPaid).Sum(p => p.AmountPaid ?? 0);
    }
}
