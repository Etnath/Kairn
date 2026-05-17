using Kairn.Application.Features.GL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kairn.Infrastructure.Jobs;

public class ExchangeRateRefreshJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ExchangeRateRefreshJob> logger) : BackgroundService
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
                logger.LogError(ex, "Unhandled error in ExchangeRateRefreshJob");
                await Task.Delay(TimeSpan.FromMinutes(10), ct);
            }
        }
    }

    private Task WaitUntilMidnightAsync(CancellationToken ct)
    {
        var now = DateTime.Now;
        var next = now.Date.AddDays(1); // next midnight
        var delay = next - now;
        logger.LogInformation("ExchangeRateRefreshJob sleeping {Delay:hh\\:mm\\:ss} until midnight", delay);
        return Task.Delay(delay, ct);
    }

    private async Task RunAsync(CancellationToken ct)
    {
        logger.LogInformation("ExchangeRateRefreshJob refreshing rates at {Now}", DateTimeOffset.Now);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IExchangeRateService>();
            await svc.RefreshRatesAsync(ct);
            logger.LogInformation("Exchange rates refreshed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exchange rate refresh failed");
        }
    }
}
