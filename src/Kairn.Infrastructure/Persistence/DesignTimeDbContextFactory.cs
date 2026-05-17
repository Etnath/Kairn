using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kairn.Infrastructure.Persistence;

/// <summary>
/// Used by EF Core tooling (dotnet ef migrations add) at design time.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=kairn-design.db")
            .Options;

        return new AppDbContext(options);
    }
}
