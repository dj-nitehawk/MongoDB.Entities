using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.MongoDb;
using Xunit;

namespace BookShop.Tests;

/// <summary>
/// Test fixture that uses TestContainers for MongoDB
/// </summary>
public class BookShopFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private MongoDbContainer? _mongoContainer;

    public async Task InitializeAsync()
    {
        // Start MongoDB container with replica set
        _mongoContainer = new MongoDbBuilder()
            .WithUsername("admin")
            .WithPassword("password")
            .WithReplicaSet()
            .Build();

        await _mongoContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override connection string with test container
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MongoDB"] = _mongoContainer!.GetConnectionString()
            });
        });
    }

    public new async Task DisposeAsync()
    {
        if (_mongoContainer is not null)
        {
            await _mongoContainer.DisposeAsync();
        }
        await base.DisposeAsync();
    }
}
