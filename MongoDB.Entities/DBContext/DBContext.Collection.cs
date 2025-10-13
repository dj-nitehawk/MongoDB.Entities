using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DBContext
{
    /// <summary>
    /// Creates a collection for an Entity type explicitly using the given options
    /// </summary>
    /// <typeparam name="T">The type of entity that will be stored in the created collection</typeparam>
    /// <param name="options">The options to use for collection creation</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task CreateCollectionAsync<T>(Action<CreateCollectionOptions<T>> options, CancellationToken cancellation = default) where T : IEntity
        => _dbInstance.CreateCollectionAsync(options, cancellation, Session);

    /// <summary>
    /// Deletes the collection of a given entity type as well as the join collections for that entity.
    /// <para>TIP: When deleting a collection, all relationships associated with that entity type is also deleted.</para>
    /// </summary>
    /// <typeparam name="T">The entity type to drop the collection of</typeparam>
    public Task DropCollectionAsync<T>() where T : IEntity
        => _dbInstance.DropCollectionAsync<T>(Session);
}