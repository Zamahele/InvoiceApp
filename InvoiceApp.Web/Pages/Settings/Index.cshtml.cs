using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Pages.Settings;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    [BindProperty]
    public CompanySettings Company { get; set; } = new();

    [BindProperty]
    public BankingDetails Banking { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        Company = await _db.CompanySettings.FirstOrDefaultAsync() ?? new CompanySettings();
        Banking = await _db.BankingDetails.FirstOrDefaultAsync() ?? new BankingDetails();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Company.Name = Company.Name?.Trim() ?? string.Empty;
        Banking.BankName = Banking.BankName?.Trim() ?? string.Empty;
        Banking.AccountType = Banking.AccountType?.Trim();
        Banking.AccountNumber = Banking.AccountNumber?.Trim();
        Banking.BranchCode = Banking.BranchCode?.Trim();

        if (string.IsNullOrWhiteSpace(Company.Name))
            ModelState.AddModelError("Company.Name", "Company name is required.");

        if (string.IsNullOrWhiteSpace(Banking.BankName))
            ModelState.AddModelError("Banking.BankName", "Bank name is required.");

        if (string.IsNullOrWhiteSpace(Banking.AccountType))
            ModelState.AddModelError("Banking.AccountType", "Account type is required.");

        if (string.IsNullOrWhiteSpace(Banking.AccountNumber))
            ModelState.AddModelError("Banking.AccountNumber", "Account number is required.");

        if (string.IsNullOrWhiteSpace(Banking.BranchCode))
            ModelState.AddModelError("Banking.BranchCode", "Branch code is required.");

        if (!ModelState.IsValid)
            return Page();

        var existingCompany = await _db.CompanySettings.FirstOrDefaultAsync();
        if (existingCompany == null)
        {
            _db.CompanySettings.Add(new CompanySettings
            {
                Name = Company.Name,
                AddressLine1 = Company.AddressLine1,
                AddressLine2 = Company.AddressLine2,
                City = Company.City,
                PostalCode = Company.PostalCode,
                Phone = Company.Phone
            });
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

        var existingBanking = await _db.BankingDetails.FirstOrDefaultAsync();
        if (existingBanking == null)
        {
            _db.BankingDetails.Add(new BankingDetails
            {
                BankName = Banking.BankName,
                AccountType = Banking.AccountType,
                AccountNumber = Banking.AccountNumber,
                BranchCode = Banking.BranchCode
            });
        }
        else
        {
            existingBanking.BankName = Banking.BankName;
            existingBanking.AccountType = Banking.AccountType;
            existingBanking.AccountNumber = Banking.AccountNumber;
            existingBanking.BranchCode = Banking.BranchCode;
        }

        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Settings saved.";
        return RedirectToPage();
    }
}
