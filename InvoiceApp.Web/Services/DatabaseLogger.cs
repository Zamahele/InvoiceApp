using InvoiceApp.Core.Entities;
using InvoiceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Web.Services;

/// <summary>
/// Writes Error/Critical log events to the ErrorLogs table so they are visible at
/// /Admin/ErrorLog. Uses a short-lived scope per write (AppDbContext is scoped) and
/// never throws — a logging failure must never take down the app.
/// </summary>
public sealed class DatabaseLoggerProvider : ILoggerProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DatabaseLoggerProvider(IServiceScopeFactory scopeFactory, IHttpContextAccessor httpContextAccessor)
    {
        _scopeFactory = scopeFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public ILogger CreateLogger(string categoryName) =>
        new DatabaseLogger(categoryName, _scopeFactory, _httpContextAccessor);

    public void Dispose() { }

    private sealed class DatabaseLogger : ILogger
    {
        private readonly string _category;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DatabaseLogger(string category, IServiceScopeFactory scopeFactory, IHttpContextAccessor httpContextAccessor)
        {
            _category = category;
            _scopeFactory = scopeFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        IDisposable? ILogger.BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            try
            {
                var message = formatter(state, exception);
                if (string.IsNullOrWhiteSpace(message) && exception == null) return;

                var httpContext = _httpContextAccessor.HttpContext;
                var entry = new ErrorLog
                {
                    Timestamp = DateTime.UtcNow,
                    RequestPath = httpContext?.Request.Path.Value ?? string.Empty,
                    RequestId = httpContext?.TraceIdentifier ?? string.Empty,
                    Message = $"[{_category}] {message}",
                    StackTrace = exception?.StackTrace,
                    InnerMessage = exception?.InnerException?.Message
                };

                // Fire-and-forget on a short-lived scope; never block the caller.
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await using var scope = _scopeFactory.CreateAsyncScope();
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        db.ErrorLogs.Add(entry);
                        await db.SaveChangesAsync();
                    }
                    catch
                    {
                        // Swallow: a logging failure must never affect the app.
                    }
                });
            }
            catch
            {
                // Swallow: never throw from a logger.
            }
        }
    }
}
