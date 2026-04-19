using System.Diagnostics;
using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InvoiceApp.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ILogger<ErrorModel> _logger;

    public string RequestId { get; private set; } = string.Empty;
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public ErrorModel(AppDbContext db, ILogger<ErrorModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (feature?.Error is not { } ex) return;

        _logger.LogError(ex, "Unhandled exception on {Path}", feature.Path);

        try
        {
            _db.ErrorLogs.Add(new ErrorLog
            {
                Timestamp   = DateTime.UtcNow,
                RequestPath = feature.Path ?? string.Empty,
                RequestId   = RequestId,
                Message     = ex.Message,
                StackTrace  = ex.StackTrace,
                InnerMessage = ex.InnerException?.Message
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception dbEx)
        {
            // Never let a logging failure replace the original error page
            _logger.LogWarning(dbEx, "Failed to persist error log to database");
        }
    }
}
