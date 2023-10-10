using MongoDB.Driver;
using System.Threading.Tasks;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <summary>
    /// Gets the IMongoCollection for a given IEntity type.
    /// <para>TIP: Try never to use this unless really necessary.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public static IMongoCollection<T> Collection<T>(this T _) where T : IEntity => DB.Collection<T>();

    /// <summary>
    /// Gets the collection name for this entity
    /// </summary>
    public static string CollectionName<T>(this T _) where T : IEntity
        => DB.CollectionName<T>();

    /// <summary>
    /// Drops a join collection
    /// </summary>
    /// <param name="collection"></param>
    public static Task DropAsync(this IMongoCollection<JoinRecord> collection)
        => collection.Database.DropCollectionAsync(collection.CollectionNamespace.CollectionName);
}