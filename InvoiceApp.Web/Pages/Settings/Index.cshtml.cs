using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InvoiceApp.Web.Pages.Settings;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    [BindProperty]
    public CompanySettings Company { get; set; } = new();

    [BindProperty]
    public BankingDetails Banking { get; set; } = new();

    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        Company = await _db.CompanySettings.FindAsync(1) ?? new CompanySettings { Id = 1 };
        Banking = await _db.BankingDetails.FindAsync(1) ?? new BankingDetails { Id = 1 };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var existingCompany = await _db.CompanySettings.FindAsync(1);
        if (existingCompany == null)
        {
            Company.Id = 1;
            _db.CompanySettings.Add(Company);
        }
        else
        {
            existingCompany.Name = Company.Name;
            existingCompany.AddressLine1 = Company.AddressLine1;
            existingCompany.AddressLine2 = Company.AddressLine2;
            existingCompany.City = Company.City;
            existingCompany.PostalCode = Company.PostalCode;
            existingCompany.Phone = Company.Phone;
        }

        var existingBanking = await _db.BankingDetails.FindAsync(1);
        if (existingBanking == null)
        {
            Banking.Id = 1;
            _db.BankingDetails.Add(Banking);
        }
        else
        {
            existingBanking.BankName = Banking.BankName;
            existingBanking.AccountType = Banking.AccountType;
            existingBanking.AccountNumber = Banking.AccountNumber;
            existingBanking.BranchCode = Banking.BranchCode;
        }

        await _db.SaveChangesAsync();
        StatusMessage = "Settings saved.";
        return Page();
    }
}
