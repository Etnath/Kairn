using Kairn.Application.Features.MarginAnalysis;
using Kairn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kairn.Infrastructure.Jobs;

public class MarginAlertJob(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<MarginAlertJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await WaitUntilNextRunAsync(ct);
                if (ct.IsCancellationRequested) break;
                await RunAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in MarginAlertJob");
                await Task.Delay(TimeSpan.FromMinutes(5), ct);
            }
        }
    }

    private Task WaitUntilNextRunAsync(CancellationToken ct)
    {
        var runTimeStr = config["MarginAlertJob:RunTime"] ?? "02:00";
        var runTime    = TimeSpan.Parse(runTimeStr);
        var now        = DateTime.Now;
        var next       = now.Date.Add(runTime);
        if (next <= now) next = next.AddDays(1);
        var delay = next - now;
        logger.LogInformation("MarginAlertJob sleeping {Delay:hh\\:mm\\:ss} until {Next:yyyy-MM-dd HH:mm}", delay, next);
        return Task.Delay(delay, ct);
    }

    private async Task RunAsync(CancellationToken ct)
    {
        logger.LogInformation("MarginAlertJob starting at {Now:u}", DateTimeOffset.UtcNow);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<IMarginAlertService>();

            var tenantIds = await db.ProductLines
                .Where(p => p.IsActive && p.MarginAlertThreshold.HasValue)
                .Select(p => p.TenantId)
                .Distinct()
                .ToListAsync(ct);

            var month = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);

            foreach (var tenantId in tenantIds)
            {
                await svc.RunCheckAsync(tenantId, month, ct);
                logger.LogInformation(
                    "MarginAlertJob: checked tenant {TenantId} for {Month:yyyy-MM}",
                    tenantId, month);
            }

            logger.LogInformation("MarginAlertJob completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MarginAlertJob run failed");
        }
    }
}
