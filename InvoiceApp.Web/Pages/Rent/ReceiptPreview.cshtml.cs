using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Pages.Rent;

public class ReceiptPreviewModel : PageModel
{
    private readonly AppDbContext _db;

    public ReceiptPreviewModel(AppDbContext db) => _db = db;

    public RentPayment Payment { get; set; } = null!;
    public RentSettings? RentSettings { get; set; }
    public BankingDetails? Banking { get; set; }

    public async Task<IActionResult> OnGetAsync(int paymentId)
    {
        var payment = await _db.RentPayments
            .Include(p => p.Room)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null || !payment.IsPaid)
            return NotFound();

        Payment = payment;
        RentSettings = await _db.RentSettings.FirstOrDefaultAsync();
        Banking = await _db.BankingDetails.FirstOrDefaultAsync();

        return Page();
    }
}
