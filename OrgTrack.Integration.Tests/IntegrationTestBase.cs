using Microsoft.Extensions.DependencyInjection;
using OrgTrack.Infrastructure.Persistence;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace OrgTrack.Integration.Tests;

public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly HttpClient Client;
    protected readonly CustomWebApplicationFactory Factory;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Clear database before each test
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrgTrackDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }

    /// <summary>
    /// Set the current user by sending the Test-User-Id header.
    /// </summary>
    protected void AuthenticateAs(Guid userId)
    {
        Client.DefaultRequestHeaders.Remove("Test-User-Id");
        Client.DefaultRequestHeaders.Add("Test-User-Id", userId.ToString());
    }

    /// <summary>
    /// Executes an action inside a database scope to seed or verify data.
    /// </summary>
    protected async Task ExecuteInDbAsync(Func<OrgTrackDbContext, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrgTrackDbContext>();
        await action(dbContext);
    }
}
