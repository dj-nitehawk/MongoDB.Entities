using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        internal static IMongoCollection<JoinRecord> GetRefCollection<T>(string name) where T : IEntity
        {
            //no support for multi-tenancy :-(
            return Database<T>(null).GetCollection<JoinRecord>(name);
        }

        /// <summary>
        /// Gets the IMongoCollection for a given IEntity type.
        /// <para>TIP: Try never to use this unless really necessary.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static IMongoCollection<T> Collection<T>(string tenantPrefix) where T : IEntity
        {
            return Cache<T>.Collection(tenantPrefix);
        }

        /// <summary>
        /// Gets the collection name for a given entity type
        /// </summary>
        /// <typeparam name="T">The type of entity to get the collection name for</typeparam>
        public static string CollectionName<T>() where T : IEntity
        {
            return Cache<T>.CollectionName;
        }

        /// <summary>
        /// Creates a collection for an Entity type explicitly using the given options
        /// </summary>
        /// <typeparam name="T">The type of entity that will be stored in the created collection</typeparam>
        /// <param name="options">The options to use for collection creation</param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public static Task CreateCollectionAsync<T>(Action<CreateCollectionOptions<T>> options, string tenantPrefix, CancellationToken cancellation = default, IClientSessionHandle session = null) where T : IEntity
        {
            //todo: tenant prefix optional 
            var opts = new CreateCollectionOptions<T>();
            options(opts);
            return session == null
                   ? Cache<T>.Collection(tenantPrefix).Database.CreateCollectionAsync(Cache<T>.CollectionName, opts, cancellation)
                   : Cache<T>.Collection(tenantPrefix).Database.CreateCollectionAsync(session, Cache<T>.CollectionName, opts, cancellation);
        }

        /// <summary>
        /// Deletes the collection of a given entity type as well as the join collections for that entity.
        /// <para>TIP: When deleting a collection, all relationships associated with that entity type is also deleted.</para>
        /// </summary>
        /// <typeparam name="T">The entity type to drop the collection of</typeparam>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public static async Task DropCollectionAsync<T>(string tenantPrefix, IClientSessionHandle session = null) where T : IEntity
        {
            //todo: tenant prefix optional 

            var tasks = new List<Task>();
            var db = Database<T>(tenantPrefix);
            var collName = CollectionName<T>();
            var options = new ListCollectionNamesOptions
            {
                Filter = "{$and:[{name:/~/},{name:/" + collName + "/}]}"
            };

            foreach (var cName in await db.ListCollectionNames(options).ToListAsync().ConfigureAwait(false))
            {
                tasks.Add(
                    session == null
                    ? db.DropCollectionAsync(cName)
                    : db.DropCollectionAsync(session, cName));
            }

            tasks.Add(
                session == null
                ? db.DropCollectionAsync(collName)
                : db.DropCollectionAsync(session, collName));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
