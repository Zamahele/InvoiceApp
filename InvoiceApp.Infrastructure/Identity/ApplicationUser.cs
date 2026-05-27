using InvoiceApp.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace InvoiceApp.Infrastructure.Identity;

/// <summary>
/// The login account. One account belongs to exactly one <see cref="Company"/>
/// (the tenant), so the account effectively *is* the company.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
}
