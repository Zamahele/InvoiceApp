using System.Security.Claims;
using InvoiceApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace InvoiceApp.Web.Tenancy;

/// <summary>
/// Adds the tenant's <c>CompanyId</c> as a claim on every sign-in, so
/// <see cref="CurrentCompanyProvider"/> can resolve the current company from the
/// auth cookie without an extra lookup.
/// </summary>
public class AppUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public AppUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(CurrentCompanyProvider.CompanyIdClaim, user.CompanyId.ToString()));
        return identity;
    }
}
