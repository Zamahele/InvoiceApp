using System.ComponentModel.DataAnnotations;
using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using InvoiceApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InvoiceApp.Web.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AppDbContext _db;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AppDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, Display(Name = "Company name")]
        public string CompanyName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password), Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Invoicing")]
        public bool EnableInvoicing { get; set; } = true;

        [Display(Name = "Rent tracking")]
        public bool EnableRentTracking { get; set; }
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        Input.CompanyName = Input.CompanyName?.Trim() ?? string.Empty;
        Input.Email = Input.Email?.Trim() ?? string.Empty;

        if (!Input.EnableInvoicing && !Input.EnableRentTracking)
            ModelState.AddModelError(string.Empty, "Select at least one service to use.");

        if (!ModelState.IsValid)
            return Page();

        // Create the tenant first, then the login account that owns it.
        var company = new Company
        {
            Name = Input.CompanyName,
            InvoicingEnabled = Input.EnableInvoicing,
            RentTrackingEnabled = Input.EnableRentTracking
        };
        _db.Companies.Add(company);
        await _db.SaveChangesAsync();

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            CompanyId = company.Id
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            // Roll back the orphaned company so a retry can reuse the name/email.
            _db.Companies.Remove(company);
            await _db.SaveChangesAsync();

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToPage("/Index");
    }
}
