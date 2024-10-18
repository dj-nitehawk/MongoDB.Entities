using System.Threading;
using System.Threading.Tasks;
using Testcontainers.MongoDb;

public static class TestDatabase
{
    private static SemaphoreSlim _semaphore = new(1, 1);
    private static MongoDbContainer? _testContainer;

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
        if (_testContainer != null)
        {
            return _testContainer;
        }
        _testContainer = new MongoDbBuilder()
                         .WithPortBinding(27017)
                         .WithPassword("username")
                         .WithUsername("password")
                         .WithReplicaSet()
                         .Build();

        await _testContainer.StartAsync();
        
        return _testContainer;
    }
}