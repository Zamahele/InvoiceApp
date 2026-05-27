using InvoiceApp.Web.Tenancy;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InvoiceApp.Web.Pages;

public class IndexModel : PageModel
{
    private readonly CompanyContext _companyContext;

    public IndexModel(CompanyContext companyContext) => _companyContext = companyContext;

    public string CompanyName { get; private set; } = string.Empty;
    public bool InvoicingEnabled { get; private set; }
    public bool RentTrackingEnabled { get; private set; }

    public async Task OnGetAsync()
    {
        var company = await _companyContext.GetAsync();
        if (company != null)
        {
            CompanyName = company.Name;
            InvoicingEnabled = company.InvoicingEnabled;
            RentTrackingEnabled = company.RentTrackingEnabled;
        }
    }
}
