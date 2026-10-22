using InvoiceApp.Infrastructure.Services;

namespace InvoiceApp.Web.Services;

public sealed class RecurringInvoiceWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringInvoiceWorker> _logger;

    public RecurringInvoiceWorker(IServiceScopeFactory scopeFactory, ILogger<RecurringInvoiceWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunDueInvoicesAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunDueInvoicesAsync(stoppingToken);
    }

    private async Task RunDueInvoicesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<RecurringInvoiceService>();
            var generatedCount = await service.GenerateDueInvoicesAsync(cancellationToken: cancellationToken);
            if (generatedCount > 0)
                _logger.LogInformation("Generated {GeneratedCount} recurring invoice(s).", generatedCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Recurring invoice generation failed.");
        }
    }
}