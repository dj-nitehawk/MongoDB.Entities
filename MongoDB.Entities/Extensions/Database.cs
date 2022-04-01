using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <summary>
    /// Gets the IMongoDatabase for the given entity type
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    [Obsolete("This method returns the current Context", error: true)]
    public static IMongoDatabase Database<T>(this T _, string tenantPrefix) where T : IEntity => DB.Database<T>();

    /// <summary>
    /// Gets the name of the database this entity is attached to. Returns name of default database if not specifically attached.
    /// </summary>
    [Obsolete("This method returns the current DatabaseName in the Context")]
    public static string DatabaseName<T>(this T _, string tenantPrefix) => DB.DatabaseName<T>();

    /// <summary>
    /// Pings the mongodb server to check if it's still connectable
    /// </summary>
    /// <param name="timeoutSeconds">The number of seconds to keep trying</param>
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
    public static async Task<bool> ExistsAsync(this IMongoDatabase db, int timeoutSeconds = 5)
    {
        using (var cts = new CancellationTokenSource(timeoutSeconds * 1000))
        {
            try
            {
                var dbs = await (await db.Client.ListDatabaseNamesAsync(cts.Token).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
                return dbs.Contains(db.DatabaseNamespace.DatabaseName);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
