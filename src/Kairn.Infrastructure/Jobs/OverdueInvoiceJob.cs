using Kairn.Application.Features.AP;
using Kairn.Application.Features.AR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kairn.Infrastructure.Jobs;

public class OverdueInvoiceJob(
    IServiceScopeFactory scopeFactory,
    ILogger<OverdueInvoiceJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await WaitUntilMidnightAsync(ct);
                if (ct.IsCancellationRequested) break;
                await RunAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in OverdueInvoiceJob");
                await Task.Delay(TimeSpan.FromMinutes(5), ct);
            }
        }
    }

    private Task WaitUntilMidnightAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var nextMidnight = now.Date.AddDays(1);
        var delay = nextMidnight - now;
        logger.LogInformation("OverdueInvoiceJob sleeping {Delay:hh\\:mm\\:ss} until {Next:yyyy-MM-dd HH:mm} UTC", delay, nextMidnight);
        return Task.Delay(delay, ct);
    }

    private async Task RunAsync(CancellationToken ct)
    {
        logger.LogInformation("OverdueInvoiceJob starting at {Now:u}", DateTimeOffset.UtcNow);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var invoiceSvc = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
            await invoiceSvc.MarkAllOverdueAsync(ct);
            var billSvc = scope.ServiceProvider.GetRequiredService<IBillService>();
            await billSvc.MarkAllOverdueAsync(ct);
            logger.LogInformation("OverdueInvoiceJob completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OverdueInvoiceJob run failed");
        }
    }
}
