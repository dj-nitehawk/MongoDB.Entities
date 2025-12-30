using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <summary>
    /// Pings the mongodb server to check if it's still connectable
    /// </summary>
    /// <param name="timeoutSeconds">The number of seconds to keep trying</param>
    /// <param name="db"></param>
    public static async Task<bool> IsAccessibleAsync(this IMongoDatabase db, int timeoutSeconds = 5)
    {
        using var cts = new CancellationTokenSource(timeoutSeconds * 1000);

        try
        {
            var res = await db.RunCommandAsync((Command<BsonDocument>)"{ping:1}", null, cts.Token).ConfigureAwait(false);

            return res["ok"] == 1;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Checks to see if the database already exists on the mongodb server
    /// </summary>
    /// <param name="timeoutSeconds">The number of seconds to keep trying</param>
    /// <param name="db"></param>
    public static async Task<bool> ExistsAsync(this IMongoDatabase db, int timeoutSeconds = 5)
    {
        using var cts = new CancellationTokenSource(timeoutSeconds * 1000);

        try
        {
            var dbs = await (await db.Client.ListDatabaseNamesAsync(cts.Token).ConfigureAwait(false)).ToListAsync(cts.Token).ConfigureAwait(false);

            return dbs.Contains(db.DatabaseNamespace.DatabaseName);
        }
        catch (Exception)
        {
            return false;
        }
    }
}