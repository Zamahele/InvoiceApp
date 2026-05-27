using System.Security.Claims;
using InvoiceApp.Infrastructure.Tenancy;

namespace InvoiceApp.Web.Tenancy;

/// <summary>
/// Reads the current tenant id from the signed-in user's <c>CompanyId</c> claim.
/// Returns null when unauthenticated (e.g. the login/register pages), which makes
/// the DbContext bypass tenant filters.
/// </summary>
public class CurrentCompanyProvider : ICurrentCompanyProvider
{
    public const string CompanyIdClaim = "CompanyId";

    private readonly IHttpContextAccessor _accessor;

    public CurrentCompanyProvider(IHttpContextAccessor accessor) => _accessor = accessor;

    public int? CompanyId
    {
        get
        {
            var value = _accessor.HttpContext?.User?.FindFirstValue(CompanyIdClaim);
            return int.TryParse(value, out var id) ? id : null;
        }
    }
}
