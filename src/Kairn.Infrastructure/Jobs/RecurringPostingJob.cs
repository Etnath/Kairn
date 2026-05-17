using Kairn.Application.Features.GL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kairn.Infrastructure.Jobs;

public class RecurringPostingJob(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<RecurringPostingJob> logger) : BackgroundService
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
                logger.LogError(ex, "Unhandled error in RecurringPostingJob");
                await Task.Delay(TimeSpan.FromMinutes(5), ct);
            }
        }
    }

    private Task WaitUntilNextRunAsync(CancellationToken ct)
    {
        var runTimeStr = config["RecurringJob:RunTime"] ?? "00:30";
        var runTime = TimeSpan.Parse(runTimeStr);

        var now = DateTime.Now;
        var next = now.Date.Add(runTime);
        if (now >= next) next = next.AddDays(1);

        var delay = next - now;
        logger.LogInformation("RecurringPostingJob sleeping {Delay:hh\\:mm\\:ss} until {Next:yyyy-MM-dd HH:mm}", delay, next);
        return Task.Delay(delay, ct);
    }

    private async Task RunAsync(CancellationToken ct)
    {
        logger.LogInformation("RecurringPostingJob starting run at {Now}", DateTimeOffset.Now);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IRecurringEntryService>();
            await svc.PostDueEntriesAsync(ct);
            logger.LogInformation("RecurringPostingJob completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RecurringPostingJob run failed");
        }
    }
}
