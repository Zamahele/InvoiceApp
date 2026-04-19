using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Pages.Admin;

public class ErrorLogModel : PageModel
{
    private readonly AppDbContext _db;

    public ErrorLogModel(AppDbContext db) => _db = db;

    public List<ErrorLog> Errors { get; private set; } = new();
    public int TotalCount { get; private set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize => 20;
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public async Task OnGetAsync()
    {
        TotalCount = await _db.ErrorLogs.CountAsync();
        Errors = await _db.ErrorLogs
            .OrderByDescending(e => e.Timestamp)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var entry = await _db.ErrorLogs.FindAsync(id);
        if (entry != null)
        {
            _db.ErrorLogs.Remove(entry);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClearAsync()
    {
        await _db.ErrorLogs.ExecuteDeleteAsync();
        return RedirectToPage();
    }
}
