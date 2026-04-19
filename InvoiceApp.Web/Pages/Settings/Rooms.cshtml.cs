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

    public List<Property> Properties { get; set; } = new();
    public List<Room> Rooms { get; set; } = new();

    // Add property
    [BindProperty] public string NewPropertyName { get; set; } = string.Empty;
    [BindProperty] public string NewPropertyAgent { get; set; } = string.Empty;
    [BindProperty] public string? NewPropertyPhone { get; set; }
    [BindProperty] public string? NewPropertyAddress1 { get; set; }
    [BindProperty] public string? NewPropertyAddress2 { get; set; }
    [BindProperty] public string? NewPropertyCity { get; set; }
    [BindProperty] public string? NewPropertyPostalCode { get; set; }

    // Edit property
    [BindProperty] public int EditPropertyId { get; set; }
    [BindProperty] public string EditPropertyName { get; set; } = string.Empty;
    [BindProperty] public string EditPropertyAgent { get; set; } = string.Empty;
    [BindProperty] public string? EditPropertyPhone { get; set; }
    [BindProperty] public string? EditPropertyAddress1 { get; set; }
    [BindProperty] public string? EditPropertyAddress2 { get; set; }
    [BindProperty] public string? EditPropertyCity { get; set; }
    [BindProperty] public string? EditPropertyPostalCode { get; set; }

    // Add room
    [BindProperty] public string NewName { get; set; } = string.Empty;
    [BindProperty] public string NewTenantName { get; set; } = string.Empty;
    [BindProperty] public string NewTenantPhone { get; set; } = string.Empty;
    [BindProperty] public decimal NewRentAmount { get; set; }
    [BindProperty] public int? NewPropertyIdLink { get; set; }

    // Edit room
    [BindProperty] public int EditId { get; set; }
    [BindProperty] public string EditName { get; set; } = string.Empty;
    [BindProperty] public string EditTenantName { get; set; } = string.Empty;
    [BindProperty] public string EditTenantPhone { get; set; } = string.Empty;
    [BindProperty] public decimal EditRentAmount { get; set; }
    [BindProperty] public int? EditPropertyIdLink { get; set; }

    [TempData] public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        Properties = await _db.Properties.OrderBy(p => p.Name).ToListAsync();
        Rooms = await _db.Rooms.Include(r => r.Property).OrderBy(r => r.Name).ToListAsync();
    }

    public async Task<IActionResult> OnPostAddPropertyAsync()
    {
        NewPropertyName = NewPropertyName?.Trim() ?? string.Empty;
        NewPropertyAgent = NewPropertyAgent?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(NewPropertyName))
            ModelState.AddModelError("NewPropertyName", "Property name is required.");
        if (string.IsNullOrWhiteSpace(NewPropertyAgent))
            ModelState.AddModelError("NewPropertyAgent", "Agent name is required.");

        if (!ModelState.IsValid)
        {
            Properties = await _db.Properties.OrderBy(p => p.Name).ToListAsync();
            Rooms = await _db.Rooms.Include(r => r.Property).OrderBy(r => r.Name).ToListAsync();
            return Page();
        }

        _db.Properties.Add(new Property
        {
            Name = NewPropertyName,
            AgentName = NewPropertyAgent,
            AgentPhone = NewPropertyPhone?.Trim(),
            AddressLine1 = NewPropertyAddress1?.Trim(),
            AddressLine2 = NewPropertyAddress2?.Trim(),
            City = NewPropertyCity?.Trim(),
            PostalCode = NewPropertyPostalCode?.Trim()
        });
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Property added.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditPropertyAsync()
    {
        var property = await _db.Properties.FindAsync(EditPropertyId);
        if (property == null) return NotFound();

        EditPropertyName = EditPropertyName?.Trim() ?? string.Empty;
        EditPropertyAgent = EditPropertyAgent?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(EditPropertyName))
            ModelState.AddModelError("EditPropertyName", "Property name is required.");
        if (string.IsNullOrWhiteSpace(EditPropertyAgent))
            ModelState.AddModelError("EditPropertyAgent", "Agent name is required.");

        if (!ModelState.IsValid)
        {
            Properties = await _db.Properties.OrderBy(p => p.Name).ToListAsync();
            Rooms = await _db.Rooms.Include(r => r.Property).OrderBy(r => r.Name).ToListAsync();
            return Page();
        }

        property.Name = EditPropertyName;
        property.AgentName = EditPropertyAgent;
        property.AgentPhone = EditPropertyPhone?.Trim();
        property.AddressLine1 = EditPropertyAddress1?.Trim();
        property.AddressLine2 = EditPropertyAddress2?.Trim();
        property.City = EditPropertyCity?.Trim();
        property.PostalCode = EditPropertyPostalCode?.Trim();
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Property updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        NewName = NewName?.Trim() ?? string.Empty;
        NewTenantName = NewTenantName?.Trim() ?? string.Empty;
        NewTenantPhone = NewTenantPhone?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(NewName))
            ModelState.AddModelError("NewName", "Room name is required.");
        if (NewRentAmount < 0)
            ModelState.AddModelError("NewRentAmount", "Rent amount cannot be negative.");

        if (!ModelState.IsValid)
        {
            Properties = await _db.Properties.OrderBy(p => p.Name).ToListAsync();
            Rooms = await _db.Rooms.Include(r => r.Property).OrderBy(r => r.Name).ToListAsync();
            return Page();
        }

        _db.Rooms.Add(new Room
        {
            Name = NewName,
            TenantName = NewTenantName,
            TenantPhone = NewTenantPhone,
            RentAmount = NewRentAmount,
            IsActive = true,
            PropertyId = NewPropertyIdLink
        });
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Room added.";
        return RedirectToPage();
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
            Properties = await _db.Properties.OrderBy(p => p.Name).ToListAsync();
            Rooms = await _db.Rooms.Include(r => r.Property).OrderBy(r => r.Name).ToListAsync();
            return Page();
        }

        room.Name = EditName;
        room.TenantName = EditTenantName;
        room.TenantPhone = EditTenantPhone;
        room.RentAmount = EditRentAmount;
        room.PropertyId = EditPropertyIdLink;
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Room updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null) return NotFound();

        room.IsActive = !room.IsActive;
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = room.IsActive ? $"'{room.Name}' activated." : $"'{room.Name}' deactivated.";
        return RedirectToPage();
    }
}
