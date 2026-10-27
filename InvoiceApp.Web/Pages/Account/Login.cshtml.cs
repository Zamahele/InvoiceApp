using System.ComponentModel.DataAnnotations;
using System.Text;
using Fido2NetLib;
using Fido2NetLib.Objects;
using InvoiceApp.Infrastructure.Data;
using InvoiceApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly IFido2 _fido2;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        IFido2 fido2)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _db = db;
        _fido2 = fido2;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public void OnGet(string? returnUrl = null) => ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        if (!ModelState.IsValid)
            return Page();

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email.Trim(), Input.Password, Input.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
            return LocalRedirect(returnUrl ?? "/");

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return Page();
    }

    public async Task<IActionResult> OnPostPasskeyOptionsAsync([FromForm] string? email)
    {
        if (!IsPasskeyOriginAllowed())
            return PasskeyError("Passkeys require a secure HTTPS connection. On localhost, HTTP is allowed for development.", StatusCodes.Status400BadRequest);

        var normalizedEmail = email?.Trim();
        var user = string.IsNullOrWhiteSpace(normalizedEmail)
            ? null
            : await _userManager.FindByEmailAsync(normalizedEmail);

        if (user != null && !user.PasskeysEnabled)
            return PasskeyError("Passkey sign-in is unavailable for this account.", StatusCodes.Status401Unauthorized);

        if (user == null && !await _db.PasskeyCredentials.AnyAsync(credential =>
            _userManager.Users.Any(account => account.Id == credential.UserId && account.PasskeysEnabled)))
            return PasskeyError("Passkey sign-in is unavailable for this account.", StatusCodes.Status401Unauthorized);

        var credentials = user == null
            ? new List<InvoiceApp.Core.Entities.PasskeyCredential>()
            : await _db.PasskeyCredentials.Where(item => item.UserId == user.Id).ToListAsync();

        var options = _fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = credentials
                .Select(item => new PublicKeyCredentialDescriptor(DecodeBase64Url(item.CredentialId)))
                .ToList(),
            UserVerification = UserVerificationRequirement.Required
        });

        HttpContext.Session.SetString("passkey.login.options", options.ToJson());
        HttpContext.Session.SetString("passkey.login.email", normalizedEmail ?? string.Empty);
        return new JsonResult(options);
    }

    public async Task<IActionResult> OnPostPasskeyLoginAsync(
        [FromBody] AuthenticatorAssertionRawResponse assertion,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsPasskeyOriginAllowed())
            return PasskeyError("Passkeys require a secure HTTPS connection. On localhost, HTTP is allowed for development.", StatusCodes.Status400BadRequest);

        var optionsJson = HttpContext.Session.GetString("passkey.login.options");
        var expectedEmail = HttpContext.Session.GetString("passkey.login.email");
        HttpContext.Session.Remove("passkey.login.options");
        HttpContext.Session.Remove("passkey.login.email");
        if (string.IsNullOrWhiteSpace(optionsJson))
            return PasskeyError("The passkey sign-in expired. Please try again.", StatusCodes.Status400BadRequest);

        var credentialId = EncodeBase64Url(assertion.RawId);
        var credential = await _db.PasskeyCredentials.FirstOrDefaultAsync(
            item => item.CredentialId == credentialId,
            cancellationToken);
        if (credential == null)
            return PasskeyError("Passkey sign-in failed. Try again or sign in with your password.", StatusCodes.Status401Unauthorized);

        var user = await _userManager.FindByIdAsync(credential.UserId);
        if (user == null || !user.PasskeysEnabled || (!string.IsNullOrWhiteSpace(expectedEmail)
            && !string.Equals(user.Email, expectedEmail, StringComparison.OrdinalIgnoreCase)))
            return PasskeyError("Passkey sign-in failed. Try again or sign in with your password.", StatusCodes.Status401Unauthorized);

        try
        {
            var options = AssertionOptions.FromJson(optionsJson);
            IsUserHandleOwnerOfCredentialIdAsync ownerCheck = (args, _) => Task.FromResult(
                credential.CredentialId == EncodeBase64Url(args.CredentialId)
                && args.UserHandle != null
                && credential.UserHandle.SequenceEqual(args.UserHandle));

            var result = await _fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = assertion,
                OriginalOptions = options,
                StoredPublicKey = credential.PublicKey,
                StoredSignatureCounter = checked((uint)Math.Clamp(credential.SignatureCounter, 0, uint.MaxValue)),
                IsUserHandleOwnerOfCredentialIdCallback = ownerCheck
            }, cancellationToken: cancellationToken);

            credential.SignatureCounter = result.SignCount;
            credential.LastUsedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            await _signInManager.SignInAsync(user, isPersistent: true);
            var redirectUrl = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Page("/Index")!;
            return new JsonResult(new { success = true, redirectUrl });
        }
        catch (Exception exception) when (exception is Fido2VerificationException or FormatException or InvalidOperationException)
        {
            return PasskeyError("Passkey verification failed. Try again or sign in with your password.", StatusCodes.Status401Unauthorized);
        }
    }

    private bool IsPasskeyOriginAllowed() => Request.IsHttps
        || (HttpContext.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            && HttpContext.Request.Host.Port == 5000);

    private JsonResult PasskeyError(string message, int statusCode) => new(new { success = false, message })
    {
        StatusCode = statusCode
    };

    private static string EncodeBase64Url(byte[] value) => Convert.ToBase64String(value)
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');

    private static byte[] DecodeBase64Url(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');
        return Convert.FromBase64String(base64);
    }
}
