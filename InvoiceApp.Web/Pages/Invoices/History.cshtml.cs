using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Pages.Invoices;

public class HistoryModel : PageModel
{
    private readonly AppDbContext _db;
    private const int PageSize = 10;

    public HistoryModel(AppDbContext db) => _db = db;

    public List<Invoice> Invoices { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public DateTime? DateFrom { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public DateTime? DateTo { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task OnGetAsync()
    {
        CurrentPage = PageNumber < 1 ? 1 : PageNumber;
        
        var query = _db.Invoices.AsQueryable();

        // Search by invoice number or client name
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var searchLower = SearchTerm.ToLower();
            query = query.Where(i => 
                i.InvoiceNumber.ToLower().Contains(searchLower) ||
                i.ClientName.ToLower().Contains(searchLower));
        }

        // Filter by date range
        if (DateFrom.HasValue)
        {
            query = query.Where(i => i.InvoiceDate >= DateFrom.Value);
        }

        if (DateTo.HasValue)
        {
            var dateToEnd = DateTo.Value.AddDays(1); // Include entire day
            query = query.Where(i => i.InvoiceDate < dateToEnd);
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync();
        TotalPages = (int)Math.Ceiling((double)totalCount / PageSize);

        // Apply sorting and pagination
        Invoices = await query
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var invoice = await _db.Invoices.Include(i => i.LineItems).FirstOrDefaultAsync(i => i.Id == id);
        if (invoice != null)
        {
            _db.Invoices.Remove(invoice);
            await _db.SaveChangesAsync();
        }
        
        // Redirect with search/filter parameters preserved
        return RedirectToPage(new 
        { 
            searchTerm = SearchTerm, 
            dateFrom = DateFrom, 
            dateTo = DateTo, 
            pageNumber = CurrentPage 
        });
    }
}
