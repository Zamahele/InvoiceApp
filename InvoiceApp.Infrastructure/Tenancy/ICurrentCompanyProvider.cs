namespace InvoiceApp.Infrastructure.Tenancy;

/// <summary>
/// Supplies the tenant (company) id for the current request. Implemented in the
/// web layer by reading the signed-in user's claim. <see cref="Data.AppDbContext"/>
/// uses it for global query filters and insert stamping.
///
/// <para>When <see cref="CompanyId"/> is null (e.g. startup migrations, background
/// work, or unit tests constructing the context without a provider) tenant query
/// filters are bypassed.</para>
/// </summary>
public interface ICurrentCompanyProvider
{
    int? CompanyId { get; }
}
