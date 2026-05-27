using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InvoiceApp.Web.Tenancy;

public enum Service
{
    Invoicing,
    RentTracking
}

/// <summary>
/// Blocks access to a page whose feature the current company has not enabled.
/// Applied to the /Invoices and /Rent folders via Razor Pages conventions in
/// Program.cs. Unauthenticated requests are left to the auth pipeline.
/// </summary>
public class RequireServiceFilter : IAsyncPageFilter
{
    private readonly Service _service;

    public RequireServiceFilter(Service service) => _service = service;

    public async Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => await Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var companyContext = context.HttpContext.RequestServices.GetRequiredService<CompanyContext>();
        var company = await companyContext.GetAsync();

        var enabled = company is not null && _service switch
        {
            Service.Invoicing => company.InvoicingEnabled,
            Service.RentTracking => company.RentTrackingEnabled,
            _ => false
        };

        if (!enabled)
        {
            context.Result = new RedirectToPageResult("/Index");
            return;
        }

        await next();
    }
}
