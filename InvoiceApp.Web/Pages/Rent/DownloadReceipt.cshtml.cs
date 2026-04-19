using InvoiceApp.Infrastructure.Data;
using InvoiceApp.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Pages.Rent;

public class DownloadReceiptModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly RentReceiptPdfService _pdfService;

    public DownloadReceiptModel(AppDbContext db, RentReceiptPdfService pdfService)
    {
        _db = db;
        _pdfService = pdfService;
    }

    public async Task<IActionResult> OnGetAsync(int paymentId)
    {
        var payment = await _db.RentPayments
            .Include(p => p.Room).ThenInclude(r => r.Property)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null || !payment.IsPaid)
            return NotFound();

        var banking = await _db.BankingDetails.FirstOrDefaultAsync();
        var pdf = _pdfService.Generate(payment, banking);

        var roomSlug = payment.Room.Name.Replace(" ", "-").ToLowerInvariant();
        var filename = $"receipt-{roomSlug}-{payment.Year}-{payment.Month:D2}.pdf";

        return File(pdf, "application/pdf", filename);
    }
}
