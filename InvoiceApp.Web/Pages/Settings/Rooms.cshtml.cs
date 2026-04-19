using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Pages.Settings;

public class RoomsModel : PageModel
{
    private readonly AppDbContext _db;

    public RoomsModel(AppDbContext db) => _db = db;

    public List<Room> Rooms { get; set; } = new();

    [BindProperty] public string NewName { get; set; } = string.Empty;
    [BindProperty] public string NewTenantName { get; set; } = string.Empty;
    [BindProperty] public string NewTenantPhone { get; set; } = string.Empty;
    [BindProperty] public decimal NewRentAmount { get; set; }

    [BindProperty] public int EditId { get; set; }
    [BindProperty] public string EditName { get; set; } = string.Empty;
    [BindProperty] public string EditTenantName { get; set; } = string.Empty;
    [BindProperty] public string EditTenantPhone { get; set; } = string.Empty;
    [BindProperty] public decimal EditRentAmount { get; set; }

    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        Rooms = await _db.Rooms.OrderBy(r => r.Name).ToListAsync();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        NewName = NewName?.Trim() ?? string.Empty;
        NewTenantName = NewTenantName?.Trim() ?? string.Empty;
        NewTenantPhone = NewTenantPhone?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(NewName))
        {
            ModelState.AddModelError("NewName", "Room name is required.");
        }

        if (NewRentAmount < 0)
        {
            ModelState.AddModelError("NewRentAmount", "Rent amount cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            Rooms = await _db.Rooms.OrderBy(r => r.Name).ToListAsync();
            return Page();
        }

        _db.Rooms.Add(new Room
        {
            Name = NewName,
            TenantName = NewTenantName,
            TenantPhone = NewTenantPhone,
            RentAmount = NewRentAmount,
            IsActive = true
        });
        await _db.SaveChangesAsync();
        StatusMessage = "Room added.";
        Rooms = await _db.Rooms.OrderBy(r => r.Name).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        var room = await _db.Rooms.FindAsync(EditId);
        if (room == null) return NotFound();

        EditName = EditName?.Trim() ?? string.Empty;
        EditTenantName = EditTenantName?.Trim() ?? string.Empty;
        EditTenantPhone = EditTenantPhone?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(EditName))
            ModelState.AddModelError("EditName", "Room name is required.");

        if (EditRentAmount < 0)
            ModelState.AddModelError("EditRentAmount", "Rent amount cannot be negative.");

        if (!ModelState.IsValid)
        {
            Rooms = await _db.Rooms.OrderBy(r => r.Name).ToListAsync();
            return Page();
        }

        room.Name = EditName;
        room.TenantName = EditTenantName;
        room.TenantPhone = EditTenantPhone;
        room.RentAmount = EditRentAmount;
        await _db.SaveChangesAsync();
        StatusMessage = "Room updated.";
        Rooms = await _db.Rooms.OrderBy(r => r.Name).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null) return NotFound();

        room.IsActive = !room.IsActive;
        await _db.SaveChangesAsync();
        StatusMessage = room.IsActive ? $"'{room.Name}' activated." : $"'{room.Name}' deactivated.";
        Rooms = await _db.Rooms.OrderBy(r => r.Name).ToListAsync();
        return Page();
    }
}
