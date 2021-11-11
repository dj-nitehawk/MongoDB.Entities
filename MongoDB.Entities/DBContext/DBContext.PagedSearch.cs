using MongoDB.Driver;

namespace MongoDB.Entities;

public partial class DBContext
{
    /// <summary>
    /// Represents an aggregation query that retrieves results with easy paging support.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <typeparam name="TId">ID type</typeparam>
    public PagedSearch<T, TId> PagedSearch<T, TId>(string? collectionName = null, IMongoCollection<T>? collection = null)
        where TId : IComparable<TId>, IEquatable<TId>
        where T : IEntity<TId>
    {
        return new PagedSearch<T, TId>(this, Collection(collectionName, collection));
    }

    /// <summary>
    /// Represents an aggregation query that retrieves results with easy paging support.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <typeparam name="TId">ID type</typeparam>
    /// <typeparam name="TProjection">The type you'd like to project the results to.</typeparam>
    public PagedSearch<T, TId, TProjection> PagedSearch<T, TId, TProjection>(string? collectionName = null, IMongoCollection<T>? collection = null)
        where TId : IComparable<TId>, IEquatable<TId>
        where T : IEntity<TId>
    {
        return new PagedSearch<T, TId, TProjection>(this, Collection(collectionName, collection));
    }
}
