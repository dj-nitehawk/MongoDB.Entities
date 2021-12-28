using MongoDB.Driver;

namespace MongoDB.Entities;

public partial class DBContext
{
    /// <summary>
    /// Represents an aggregation query that retrieves results with easy paging support.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public PagedSearch<T> PagedSearch<T>(string? collectionName = null, IMongoCollection<T>? collection = null)
    {
        return new PagedSearch<T>(this, Collection(collectionName, collection));
    }

    /// <summary>
    /// Represents an aggregation query that retrieves results with easy paging support.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <typeparam name="TProjection">The type you'd like to project the results to.</typeparam>
    public PagedSearch<T, TProjection> PagedSearch<T, TProjection>(string? collectionName = null, IMongoCollection<T>? collection = null)
    {
        return new PagedSearch<T, TProjection>(this, Collection(collectionName, collection));
    }
}
