using FluentAssertions;
using Kairn.Application.Common;
using Kairn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kairn.Tests;

[Collection("Integration")]
public class DiContainerSmokeTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DiContainerSmokeTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public void DI_container_resolves_AppDbContext()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetService<AppDbContext>();
        db.Should().NotBeNull();
    }

    [Fact]
    public void DI_container_resolves_ICurrentUserContext()
    {
        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetService<ICurrentUserContext>();
        ctx.Should().NotBeNull();
    }
}
