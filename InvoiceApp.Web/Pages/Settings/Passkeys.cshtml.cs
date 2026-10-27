using System.Text;
using Fido2NetLib;
using Fido2NetLib.Objects;
using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using InvoiceApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Pages.Settings;

public class PasskeysModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFido2 _fido2;
    private readonly ILogger<PasskeysModel> _logger;

    public PasskeysModel(AppDbContext db, UserManager<ApplicationUser> userManager, IFido2 fido2, ILogger<PasskeysModel> logger)
    {
        _db = db;
        _userManager = userManager;
        _fido2 = fido2;
        _logger = logger;
    }

    [BindProperty]
    public string PasskeyName { get; set; } = string.Empty;

    [BindProperty]
    public bool PasskeysEnabled { get; set; }

    public bool PasskeysAvailable { get; private set; }
    public List<PasskeyCredential> Credentials { get; private set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        PasskeysAvailable = IsPasskeyOriginAllowed();
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return;

        PasskeysEnabled = user.PasskeysEnabled;
        Credentials = await _db.PasskeyCredentials
            .Where(credential => credential.UserId == user.Id)
            .OrderBy(credential => credential.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostRegistrationOptionsAsync()
    {
        if (!IsPasskeyOriginAllowed())
            return PasskeyError("Passkey registration requires a secure HTTPS connection. Localhost is allowed for development.", StatusCodes.Status400BadRequest);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        if (!user.PasskeysEnabled)
            return PasskeyError("Enable passkey sign-in before adding a device.", StatusCodes.Status403Forbidden);

        var existingCredentials = await _db.PasskeyCredentials
            .Where(credential => credential.UserId == user.Id)
            .Select(credential => credential.CredentialId)
            .ToListAsync();
        var fidoUser = new Fido2User
        {
            Name = user.Email ?? user.UserName ?? user.Id,
            DisplayName = user.Email ?? user.UserName ?? "InvoiceApp user",
            Id = Encoding.UTF8.GetBytes(user.Id)
        };

        var options = _fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = fidoUser,
            ExcludeCredentials = existingCredentials
                .Select(credentialId => new PublicKeyCredentialDescriptor(DecodeBase64Url(credentialId)))
                .ToList(),
            AuthenticatorSelection = new AuthenticatorSelection
            {
                ResidentKey = ResidentKeyRequirement.Required,
                UserVerification = UserVerificationRequirement.Required
            },
            AttestationPreference = AttestationConveyancePreference.None
        });

        HttpContext.Session.SetString("passkey.registration.options", options.ToJson());
        HttpContext.Session.SetString("passkey.registration.user", user.Id);
        return new JsonResult(options);
    }

    public async Task<IActionResult> OnPostRegisterAsync(
        [FromBody] RegisterPasskeyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsPasskeyOriginAllowed())
            return PasskeyError("Passkey registration requires a secure HTTPS connection. Localhost is allowed for development.", StatusCodes.Status400BadRequest);

        var optionsJson = HttpContext.Session.GetString("passkey.registration.options");
        var expectedUserId = HttpContext.Session.GetString("passkey.registration.user");
        HttpContext.Session.Remove("passkey.registration.options");
        HttpContext.Session.Remove("passkey.registration.user");

        var user = await _userManager.GetUserAsync(User);
        if (user == null || string.IsNullOrWhiteSpace(optionsJson) || expectedUserId != user.Id)
            return PasskeyError("Passkey registration expired. Start again.", StatusCodes.Status400BadRequest);
        if (!user.PasskeysEnabled)
            return PasskeyError("Enable passkey sign-in before adding a device.", StatusCodes.Status403Forbidden);

        var name = string.IsNullOrWhiteSpace(request.Name) ? "Passkey" : request.Name.Trim();
        if (name.Length > 100)
            return PasskeyError("Passkey name must be 100 characters or fewer.", StatusCodes.Status400BadRequest);

        try
        {
            var options = CredentialCreateOptions.FromJson(optionsJson);
            IsCredentialIdUniqueToUserAsyncDelegate isUnique = async (args, token) =>
                !await _db.PasskeyCredentials.AnyAsync(
                    credential => credential.CredentialId == EncodeBase64Url(args.CredentialId), token);

            var result = await _fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
            {
                AttestationResponse = request.Response,
                OriginalOptions = options,
                IsCredentialIdUniqueToUserCallback = isUnique
            }, cancellationToken: cancellationToken);

            _db.PasskeyCredentials.Add(new PasskeyCredential
            {
                CompanyId = user.CompanyId,
                UserId = user.Id,
                Name = name,
                CredentialId = EncodeBase64Url(result.Id),
                PublicKey = result.PublicKey,
                UserHandle = Encoding.UTF8.GetBytes(user.Id),
                SignatureCounter = result.SignCount,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
            return new JsonResult(new { success = true });
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Passkey registration failed for user {UserId}.", user.Id);
            return PasskeyError("The passkey could not be registered. Please try again.", StatusCodes.Status400BadRequest);
        }
    }

    public async Task<IActionResult> OnPostRemoveAsync(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var credential = await _db.PasskeyCredentials
            .FirstOrDefaultAsync(item => item.Id == id && item.UserId == user.Id);
        if (credential == null) return NotFound();

        _db.PasskeyCredentials.Remove(credential);
        await _db.SaveChangesAsync();
        StatusMessage = "Passkey removed from this account.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        user.PasskeysEnabled = PasskeysEnabled;
        var result = await _userManager.UpdateAsync(user);
        StatusMessage = !result.Succeeded
            ? "Could not save the passkey setting. Please try again."
            : PasskeysEnabled
                ? "Passkey sign-in enabled. Add a passkey for this device."
                : "Passkey sign-in disabled. Your registered devices are kept.";
        return RedirectToPage();
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

    public sealed class RegisterPasskeyRequest
    {
        public string Name { get; set; } = string.Empty;
        public AuthenticatorAttestationRawResponse Response { get; set; } = null!;
    }
}
