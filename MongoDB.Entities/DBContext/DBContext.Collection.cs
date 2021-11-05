using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DBContext
    {
        /// <summary>
        /// Creates a collection for an Entity type explicitly using the given options
        /// </summary>
        /// <typeparam name="T">The type of entity that will be stored in the created collection</typeparam>
        /// <param name="options">The options to use for collection creation</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task CreateCollectionAsync<T>(Action<CreateCollectionOptions<T>> options, CancellationToken cancellation = default) where T : IEntity
        {
            var opts = new CreateCollectionOptions<T>();
            options(opts);
            return Session == null
                  ? Database.CreateCollectionAsync(Cache<T>().CollectionName, opts, cancellation)
                  : Database.CreateCollectionAsync(Session, Cache<T>().CollectionName, opts, cancellation);

        }

        /// <summary>
        /// Deletes the collection of a given entity type as well as the join collections for that entity.
        /// <para>TIP: When deleting a collection, all relationships associated with that entity type is also deleted.</para>
        /// </summary>
        /// <typeparam name="T">The entity type to drop the collection of</typeparam>
        public async Task DropCollectionAsync<T>() where T : IEntity
        {
            var tasks = new List<Task>();
            var db = Database;
            var collName = Cache<T>().CollectionName;
            var options = new ListCollectionNamesOptions
            {
                Filter = "{$and:[{name:/~/},{name:/" + collName + "/}]}"
            };

            foreach (var cName in await db.ListCollectionNames(options).ToListAsync().ConfigureAwait(false))
            {
                tasks.Add(
                    Session == null
                    ? db.DropCollectionAsync(cName)
                    : db.DropCollectionAsync(Session, cName));
            }

            tasks.Add(
                Session == null
                ? db.DropCollectionAsync(collName)
                : db.DropCollectionAsync(Session, collName));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
