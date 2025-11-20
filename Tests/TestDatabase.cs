using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using Testcontainers.MongoDb;

public static class TestDatabase
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static MongoDbContainer? _testContainer;
    private static int Port=27017;

    public static async Task<MongoDbContainer> CreateDatabase()
    {
        await _semaphore.WaitAsync();

        try
        {
            var database = await CreateTestDatabase();

            return database;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static async Task<MongoDbContainer> CreateTestDatabase()
    {
        _testContainer = new MongoDbBuilder()
                         .WithPortBinding(Port++)
                         .WithPassword("username")
                         .WithUsername("password")
                         .WithReplicaSet()
                         .Build();

        await _testContainer.StartAsync();

        return _testContainer;
    }
}