using BookShop.Api.Entities;
using MongoDB.Entities;

namespace BookShop.Api.Infrastructure;

public static class DatabaseSetup
{
    public static async Task InitializeAsync(string connectionString)
    {
        var db = await DB.InitAsync(
            "BookShopDb",
            MongoDB.Driver.MongoClientSettings.FromConnectionString(connectionString));

        // Create indexes
        await db.Index<Book>()
            .Key(b => b.Title, KeyType.Text)
            .CreateAsync();

        await db.Index<Author>()
            .Key(a => a.Name, KeyType.Text)
            .CreateAsync();
    }
}
