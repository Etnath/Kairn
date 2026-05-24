using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

/// <summary>
/// Singleton-safe DbContext factory for read-only dashboard queries.
/// Creates AppDbContext instances without the AuditLogInterceptor so it carries
/// no scoped dependencies and can be consumed by singleton services (Fluxor effects).
/// </summary>
public sealed class DashboardDbContextFactory(DbContextOptions<AppDbContext> options)
    : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext() => new(options);

    public Task<AppDbContext> CreateDbContextAsync(CancellationToken ct = default)
        => Task.FromResult(new AppDbContext(options));
}
