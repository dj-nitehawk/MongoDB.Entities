using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <typeparam name="T">The type of entity</typeparam>
    extension<T>(T _) where T : IEntity
    {
        /// <summary>
        /// Gets the IMongoDatabase for the given entity type
        /// </summary>
        public IMongoDatabase Database(DB? db = null)
            => DB.InstanceOrDefault(db).Database<T>();

        /// <summary>
        /// Gets the name of the database this entity is attached to. Returns name of default database if not specifically attached.
        /// </summary>
        public string DatabaseName(DB? db = null)
            => DB.InstanceOrDefault(db).DatabaseName<T>();
    }

    /// <param name="db"></param>
    extension(IMongoDatabase db)
    {
        /// <summary>
        /// Pings the mongodb server to check if it's still connectable
        /// </summary>
        /// <param name="timeoutSeconds">The number of seconds to keep trying</param>
        public async Task<bool> IsAccessibleAsync(int timeoutSeconds = 5)
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
        public async Task<bool> ExistsAsync(int timeoutSeconds = 5)
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
}