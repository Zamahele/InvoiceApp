using System.ComponentModel.DataAnnotations;
using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Pages.Settings;

public class EmailModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IDataProtector _protector;

    public EmailModel(AppDbContext db, IDataProtectionProvider dataProtection)
    {
        _db = db;
        _protector = dataProtection.CreateProtector(EmailProtection.Purpose);
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    // True when a password is already stored, so the form can show "leave blank to keep".
    public bool HasStoredPassword { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public class InputModel
    {
        [Required, Display(Name = "SMTP host")]
        public string SmtpHost { get; set; } = string.Empty;

        [Range(1, 65535), Display(Name = "Port")]
        public int SmtpPort { get; set; } = 587;

        [Display(Name = "Use SSL/TLS")]
        public bool UseSsl { get; set; } = true;

        [Required, Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        // Optional once a password is stored; required on first save.
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required, EmailAddress, Display(Name = "From email")]
        public string FromEmail { get; set; } = string.Empty;

        [Required, Display(Name = "From name")]
        public string FromName { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        var s = await _db.EmailSettings.FirstOrDefaultAsync();
        if (s != null)
        {
            Input.SmtpHost = s.SmtpHost;
            Input.SmtpPort = s.SmtpPort;
            Input.UseSsl = s.UseSsl;
            Input.Username = s.Username;
            Input.FromEmail = s.FromEmail;
            Input.FromName = s.FromName;
            HasStoredPassword = !string.IsNullOrEmpty(s.Password);
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var existing = await _db.EmailSettings.FirstOrDefaultAsync();
        HasStoredPassword = existing != null && !string.IsNullOrEmpty(existing.Password);

        // A password is only mandatory the first time (otherwise the stored one is kept).
        if (!HasStoredPassword && string.IsNullOrWhiteSpace(Input.Password))
            ModelState.AddModelError("Input.Password", "Password is required.");

        if (!ModelState.IsValid)
            return Page();

        var entity = existing ?? new EmailSettings();
        entity.SmtpHost = Input.SmtpHost.Trim();
        entity.SmtpPort = Input.SmtpPort;
        entity.UseSsl = Input.UseSsl;
        entity.Username = Input.Username.Trim();
        entity.FromEmail = Input.FromEmail.Trim();
        entity.FromName = Input.FromName.Trim();

        // Encrypt at rest; only replace the stored password when a new one is entered.
        if (!string.IsNullOrWhiteSpace(Input.Password))
            entity.Password = _protector.Protect(Input.Password);

        if (existing == null)
            _db.EmailSettings.Add(entity);

        await _db.SaveChangesAsync();
        StatusMessage = "Email settings saved.";
        return RedirectToPage();
    }
}
