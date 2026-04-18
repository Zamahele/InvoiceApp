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
        await EnsurePaymentsForMonth();
        await LoadPayments();
    }

    public async Task<IActionResult> OnPostMarkPaidAsync(int id, decimal amountPaid, string? notes)
    {
        var payment = await _db.RentPayments.FindAsync(id);
        if (payment != null)
        {
            payment.IsPaid = true;
            payment.AmountPaid = amountPaid;
            payment.PaidDate = DateTime.Today;
            payment.Notes = notes;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { Month, Year });
    }

    public async Task<IActionResult> OnPostUndoAsync(int id)
    {
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
