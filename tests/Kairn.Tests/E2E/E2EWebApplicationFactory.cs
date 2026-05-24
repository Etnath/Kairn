using Kairn.Infrastructure.Persistence;
using Kairn.Infrastructure.Persistence.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;

namespace Kairn.Tests.E2E;

/// <summary>
/// WebApplicationFactory that also starts a real Kestrel TCP listener so Playwright
/// can reach the app over HTTP.
///
/// Strategy: build the host twice from the same IHostBuilder.
///   Build 1 (testHost)  – IServer == TestServer; started and returned to
///                         WebApplicationFactory so GetTestServer() succeeds.
///   Build 2 (realHost)  – Kestrel registered after TestServer, so Kestrel wins
///                         IServer. ApplicationServices is wired manually because
///                         the WebApplicationFactory compatibility shim does not do it.
///
/// A free port is pre-allocated with TcpListener (then released) to avoid the
/// IServerAddressesFeature not being populated with port-0 dynamic binding.
///
/// Database isolation: both builds share the same temp SQLite file. We replace only
/// DbContextOptions&lt;AppDbContext&gt; and IDbContextFactory&lt;AppDbContext&gt; via ConfigureServices
/// (which runs after Program.cs). AppDbContext itself is left in place — removing it strips
/// EF Core internal registrations and prevents host startup.
/// </summary>
public sealed class E2EWebApplicationFactory : WebApplicationFactory<Program>
{
    private IHost? _realHost;
    private readonly int _port = AllocateFreePort();

    // Computed once so both builds (testHost + realHost) share the same SQLite file.
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(), $"kairn-e2e-{Guid.NewGuid():N}.db");

    public string ServerUrl => $"http://127.0.0.1:{_port}";

    private static int AllocateFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connStr = $"Data Source={_dbPath}";

        builder.ConfigureServices(services =>
        {
            // Replace only the DbContextOptions and IDbContextFactory registrations so
            // both the scoped AppDbContext and the singleton DashboardService use the
            // isolated temp SQLite file instead of kairn.db from appsettings.
            // Do NOT remove AppDbContext itself — that strips EF Core internal registrations
            // and prevents the host from starting.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextFactory<AppDbContext>>();

            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connStr)
                .Options;

            services.AddScoped<DbContextOptions<AppDbContext>>(_ => opts);
            services.AddSingleton<IDbContextFactory<AppDbContext>>(
                new DashboardDbContextFactory(opts));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Build 1: test host. The base CreateHost starts the host before returning;
        // replicate that so TestServer.Application is set before CreateClient() runs.
        var testHost = builder.Build();
        testHost.Start();

        // Build 2: add Kestrel AFTER TestServer so Kestrel wins IServer resolution.
        builder.ConfigureWebHost(b =>
            b.UseKestrel(o => o.Listen(IPAddress.Loopback, _port)));

        _realHost = builder.Build();

        // The WebApplicationFactory compatibility shim does not set
        // KestrelServerOptions.ApplicationServices; do it before Start().
        var kestrelOpts = _realHost.Services
            .GetService<IOptions<KestrelServerOptions>>()?.Value;
        if (kestrelOpts is not null)
            kestrelOpts.ApplicationServices = _realHost.Services;

        _realHost.Start();

        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _realHost?.Dispose();
        base.Dispose(disposing);
    }
}
