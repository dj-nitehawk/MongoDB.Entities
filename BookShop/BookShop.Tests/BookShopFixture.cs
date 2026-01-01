using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.MongoDb;
using Xunit;

namespace BookShop.Tests;

/// <summary>
/// Test fixture that uses TestContainers for MongoDB
/// Shared across all tests using collection fixture
/// </summary>
public class BookShopFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer;

    public BookShopFixture()
    {
        // Start MongoDB container with replica set
        _mongoContainer = new MongoDbBuilder()
            .WithUsername("admin")
            .WithPassword("password")
            .WithReplicaSet()
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override connection string with test container
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MongoDB"] = _mongoContainer.GetConnectionString()
            });
        });
    }

    public new async Task DisposeAsync()
    {
        await _mongoContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}

/// <summary>
/// Collection definition for sharing the fixture across all tests
/// </summary>
[CollectionDefinition("BookShop")]
public class BookShopCollection : ICollectionFixture<BookShopFixture>
{
}
