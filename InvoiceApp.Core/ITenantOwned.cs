namespace InvoiceApp.Core;

/// <summary>
/// Marks an entity as owned by a <see cref="Entities.Company"/> tenant. The
/// DbContext applies a global query filter on <see cref="CompanyId"/> and stamps
/// it automatically on insert, so handlers never set it by hand.
/// </summary>
public interface ITenantOwned
{
    int CompanyId { get; set; }
}
