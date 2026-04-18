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
        if (string.IsNullOrWhiteSpace(NewName))
        {
            StatusMessage = "Room name is required.";
            Rooms = await _db.Rooms.OrderBy(r => r.Name).ToListAsync();
            return Page();
        }

        _db.Rooms.Add(new Room
        {
            Name = NewName.Trim(),
            TenantName = NewTenantName.Trim(),
            TenantPhone = NewTenantPhone.Trim(),
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

        room.Name = EditName.Trim();
        room.TenantName = EditTenantName.Trim();
        room.TenantPhone = EditTenantPhone.Trim();
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
