using Kairn.Application.Features.FixedAssets;
using Kairn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kairn.Infrastructure.Jobs;

public class DepreciationJob(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<DepreciationJob> logger) : BackgroundService
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
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in DepreciationJob");
                await Task.Delay(TimeSpan.FromMinutes(5), ct);
            }
        }
    }

    private Task WaitUntilNextRunAsync(CancellationToken ct)
    {
        var runTimeStr = config["DepreciationJob:RunTime"] ?? "01:00";
        var runTime = TimeSpan.Parse(runTimeStr);

        var now = DateTime.Now;
        var today = now.Date;
        var lastDayThisMonth = new DateTime(today.Year, today.Month,
            DateTime.DaysInMonth(today.Year, today.Month));
        var scheduledToday = lastDayThisMonth.Add(runTime);

        DateTime nextRun;
        if (today == lastDayThisMonth && now < scheduledToday)
        {
            nextRun = scheduledToday;
        }
        else
        {
            // Move to last day of next month
            var firstOfNextMonth = new DateTime(today.Year, today.Month, 1).AddMonths(1);
            var lastDayNextMonth = new DateTime(firstOfNextMonth.Year, firstOfNextMonth.Month,
                DateTime.DaysInMonth(firstOfNextMonth.Year, firstOfNextMonth.Month));
            nextRun = lastDayNextMonth.Add(runTime);
        }

        var delay = nextRun - now;
        logger.LogInformation("DepreciationJob sleeping {Delay:hh\\:mm\\:ss} until {Next:yyyy-MM-dd HH:mm}", delay, nextRun);
        return Task.Delay(delay, ct);
    }

    private async Task RunAsync(CancellationToken ct)
    {
        logger.LogInformation("DepreciationJob starting at {Now:u}", DateTimeOffset.UtcNow);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<IFixedAssetService>();

            var tenantIds = await db.FixedAssets
                .Where(x => x.IsActive && !x.IsFullyDepreciated)
                .Select(x => x.TenantId)
                .Distinct()
                .ToListAsync(ct);

            var today = DateOnly.FromDateTime(DateTime.Today);

            foreach (var tenantId in tenantIds)
            {
                var result = await svc.RunDepreciationAsync(tenantId, today, "system", "System", ct);
                logger.LogInformation(
                    "DepreciationJob: tenant {TenantId} — posted {Posted}, skipped {Skipped}, failed {Failed}",
                    tenantId, result.Posted, result.Skipped, result.Failed);

                foreach (var failure in result.Failures)
                    logger.LogWarning(
                        "Depreciation failed for asset {AssetName} ({AssetId}) period {Period}: {Error}",
                        failure.AssetName, failure.AssetId, failure.Period, failure.Error);
            }

            logger.LogInformation("DepreciationJob completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DepreciationJob run failed");
        }
    }
}
