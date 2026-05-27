using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using InvoiceApp.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Tenancy;

/// <summary>
/// Loads the current tenant's <see cref="Company"/> (including its enabled
/// services) once per request. Used by the layout nav and the service-gating
/// page filter. Always reads live so a service toggle takes effect immediately.
/// </summary>
public class CompanyContext
{
    private readonly AppDbContext _db;
    private readonly ICurrentCompanyProvider _current;
    private Company? _cached;
    private bool _loaded;

    public CompanyContext(AppDbContext db, ICurrentCompanyProvider current)
    {
        _db = db;
        _current = current;
    }

    public async Task<Company?> GetAsync()
    {
        if (_loaded) return _cached;

        var id = _current.CompanyId;
        _cached = id.HasValue ? await _db.Companies.FindAsync(id.Value) : null;
        _loaded = true;
        return _cached;
    }
}
