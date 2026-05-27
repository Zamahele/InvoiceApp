using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using InvoiceApp.Infrastructure.Tenancy;
using InvoiceApp.Web.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceApp.Tests.Pages;

public class ServiceGatingTests
{
    private sealed class StubCompany : ICurrentCompanyProvider
    {
        public int? CompanyId { get; init; }
    }

    // Builds a request-scoped provider with the company seeded and a stub tenant id.
    private static IServiceProvider BuildScope(Company company, int companyId)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddScoped<ICurrentCompanyProvider>(_ => new StubCompany { CompanyId = companyId });
        services.AddScoped<CompanyContext>();
        var root = services.BuildServiceProvider();

        var scope = root.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Companies.Add(company);
        db.SaveChanges();
        return scope.ServiceProvider;
    }

    private static async Task<(bool nextCalled, PageHandlerExecutingContext ctx)> RunFilter(
        Company company, int companyId, Service service)
    {
        var sp = BuildScope(company, companyId);
        var httpContext = new DefaultHttpContext { RequestServices = sp };
        var actionContext = new ActionContext(httpContext, new RouteData(),
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor(), new ModelStateDictionary());
        var pageContext = new PageContext(actionContext);
        var filters = new List<IFilterMetadata>();

        var ctx = new PageHandlerExecutingContext(
            pageContext, filters, handlerMethod: null!,
            handlerArguments: new Dictionary<string, object?>(), handlerInstance: new object());

        var nextCalled = false;
        await new RequireServiceFilter(service).OnPageHandlerExecutionAsync(ctx, () =>
        {
            nextCalled = true;
            return Task.FromResult(new PageHandlerExecutedContext(
                pageContext, filters, handlerMethod: null!, handlerInstance: new object()));
        });

        return (nextCalled, ctx);
    }

    [Fact]
    public async Task Disabled_Service_Redirects_To_Home()
    {
        var company = new Company { Id = 1, Name = "T", InvoicingEnabled = false, RentTrackingEnabled = true };

        var (nextCalled, ctx) = await RunFilter(company, 1, Service.Invoicing);

        Assert.False(nextCalled);
        var redirect = Assert.IsType<RedirectToPageResult>(ctx.Result);
        Assert.Equal("/Index", redirect.PageName);
    }

    [Fact]
    public async Task Enabled_Service_Proceeds()
    {
        var company = new Company { Id = 1, Name = "T", InvoicingEnabled = true, RentTrackingEnabled = false };

        var (nextCalled, ctx) = await RunFilter(company, 1, Service.Invoicing);

        Assert.True(nextCalled);
        Assert.Null(ctx.Result);
    }
}
