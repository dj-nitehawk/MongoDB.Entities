using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DBInstance
{
    internal IMongoCollection<JoinRecord> GetRefCollection<T>(string name) where T : IEntity
        => Database<T>().GetCollection<JoinRecord>(name);

    /// <summary>
    /// Gets the IMongoCollection for a given IEntity type.
    /// <para>TIP: Try never to use this unless really necessary.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public IMongoCollection<T> Collection<T>() where T : IEntity
        => _mongoDatabase.GetCollection<T>(Cache<T>.CollectionName);

    /// <summary>
    /// Gets the collection name for a given entity type
    /// </summary>
    /// <typeparam name="T">The type of entity to get the collection name for</typeparam>
    public string CollectionName<T>() where T : IEntity
        => Cache<T>.CollectionName;

    /// <summary>
    /// Creates a collection for an Entity type explicitly using the given options
    /// </summary>
    /// <typeparam name="T">The type of entity that will be stored in the created collection</typeparam>
    /// <param name="options">The options to use for collection creation</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <param name="session">An optional session if using within a transaction</param>
    public Task CreateCollectionAsync<T>(Action<CreateCollectionOptions<T>> options,
                                                CancellationToken cancellation = default,
                                                IClientSessionHandle? session = null) where T : IEntity
    {
        var opts = new CreateCollectionOptions<T>();
        options(opts);

        return session == null
                   ? _mongoDatabase.CreateCollectionAsync(Cache<T>.CollectionName, opts, cancellation)
                   : _mongoDatabase.CreateCollectionAsync(session, Cache<T>.CollectionName, opts, cancellation);
    }

    /// <summary>
    /// Deletes the collection of a given entity type as well as the join collections for that entity.
    /// <para>TIP: When deleting a collection, all relationships associated with that entity type is also deleted.</para>
    /// </summary>
    /// <typeparam name="T">The entity type to drop the collection of</typeparam>
    /// <param name="session">An optional session if using within a transaction</param>
    public async Task DropCollectionAsync<T>(IClientSessionHandle? session = null) where T : IEntity
    {
        var tasks = new List<Task>();
        var db = Database<T>();
        var collName = CollectionName<T>();
        var options = new ListCollectionNamesOptions
        {
            Filter = "{$and:[{name:/~/},{name:/" + collName + "/}]}"
        };

        // ReSharper disable once MethodHasAsyncOverload
        var list = await db.ListCollectionNames(options).ToListAsync().ConfigureAwait(false);

        for (var i = 0; i < list.Count; i++)
        {
            var cName = list[i];
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