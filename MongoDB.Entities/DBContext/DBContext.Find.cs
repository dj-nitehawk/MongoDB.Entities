using MongoDB.Driver;

namespace MongoDB.Entities;

public partial class DBContext
{
    /// <summary>
    /// Starts a find command for the given entity type
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    public Find<T> Find<T>(string? collectionName = null, IMongoCollection<T>? collection = null)
    {
        return new Find<T>(this, Collection(collectionName, collection));
    }

    /// <summary>
    /// Starts a find command with projection support for the given entity type
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>    
    /// <typeparam name="TProjection">The type of the end result</typeparam>
    public Find<T, TProjection> Find<T,  TProjection>(string? collectionName = null, IMongoCollection<T>? collection = null)
    {
        return new Find<T, TProjection>(this, Collection(collectionName, collection));
    }
}
