using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DB
    {
        internal static IMongoCollection<JoinRecord> GetRefCollection<T>(string name) where T : IEntity
        {
            return GetDatabase<T>().GetCollection<JoinRecord>(name);
        }

        /// <summary>
        /// Gets the IMongoCollection for a given IEntity type.
        /// <para>TIP: Try never to use this unless really neccessary.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static IMongoCollection<T> Collection<T>() where T : IEntity
        {
            return Cache<T>.Collection;
        }

        /// <summary>
        /// Gets the IMongoCollection for a given IEntity type.
        /// <para>TIP: Try never to use this unless really neccessary.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public IMongoCollection<T> Collection<T>(bool _ = false) where T : IEntity
        {
            return Collection<T>();
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
        /// Deletes the collection of a given entity type as well as the join collections for that entity. 
        /// <para>TIP: When deleting a collection, all relationships associated with that entity type is also deleted.</para>
        /// </summary>
        /// <typeparam name="T">The entity type to drop the collection of</typeparam>
        /// <param name="session">An optional session if using within a transaction</param>
        public static void DropCollection<T>(IClientSessionHandle session = null) where T : IEntity
        {
            DropCollectionPrep<T>(
                out IMongoDatabase db,
                out string collName,
                out ListCollectionNamesOptions options);

            foreach (var cName in db.ListCollectionNames(options).ToList())
            {
                if (session == null) db.DropCollection(cName);
                else db.DropCollection(session, cName);
            }

            if (session == null) db.DropCollection(collName);
            else db.DropCollection(session, collName);
        }

        /// <summary>
        /// Deletes the collection of a given entity type as well as the join collections for that entity. 
        /// <para>TIP: When deleting a collection, all relationships associated with that entity type is also deleted.</para>
        /// </summary>
        /// <typeparam name="T">The entity type to drop the collection of</typeparam>
        /// <param name="session">An optional session if using within a transaction</param>
        public void DropCollection<T>(IClientSessionHandle session = null, bool _ = false) where T : IEntity
        {
            DropCollection<T>(session);
        }

        /// <summary>
        /// Deletes the collection of a given entity type as well as the join collections for that entity. 
        /// <para>TIP: When deleting a collection, all relationships associated with that entity type is also deleted.</para>
        /// </summary>
        /// <typeparam name="T">The entity type to drop the collection of</typeparam>
        /// <param name="session">An optional session if using within a transaction</param>
        public static async Task DropCollectionAsync<T>(IClientSessionHandle session = null) where T : IEntity
        {
            var tasks = new HashSet<Task>();

            DropCollectionPrep<T>(
                out IMongoDatabase db,
                out string collName,
                out ListCollectionNamesOptions options);

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

        /// <summary>
        /// Deletes the collection of a given entity type as well as the join collections for that entity. 
        /// <para>TIP: When deleting a collection, all relationships associated with that entity type is also deleted.</para>
        /// </summary>
        /// <typeparam name="T">The entity type to drop the collection of</typeparam>
        /// <param name="session">An optional session if using within a transaction</param>
        public Task DropCollectionAsync<T>(IClientSessionHandle session = null, bool _ = false) where T : IEntity
        {
            return DropCollectionAsync<T>(session);
        }

        private static void DropCollectionPrep<T>(out IMongoDatabase db, out string collName, out ListCollectionNamesOptions options) where T : IEntity
        {
            db = GetDatabase<T>();
            collName = CollectionName<T>();
            options = new ListCollectionNamesOptions
            {
                Filter = "{$and:[{name:/~/},{name:/" + collName + "/}]}"
            };
        }
    }
}
