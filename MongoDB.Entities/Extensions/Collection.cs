using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <summary>
    /// Gets the IMongoCollection for a given IEntity type.
    /// <para>TIP: Try never to use this unless really necessary.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="_"></param>
    /// <param name="db">The DB instance to use for this operation</param>
    public static IMongoCollection<T> Collection<T>(this T _, DB? db = null) where T : IEntity
        => DB.InstanceOrDefault(db).Collection<T>();

    /// <summary>
    /// Gets the collection name for this entity
    /// </summary>
    /// <param name="_"></param>
    /// <param name="db">The DB instance to use for this operation</param>
    public static string CollectionName<T>(this T _, DB? db = null) where T : IEntity
        => DB.InstanceOrDefault(db).CollectionName<T>();

    /// <summary>
    /// Drops a join collection
    /// </summary>
    /// <param name="collection"></param>
    public static Task DropAsync(this IMongoCollection<JoinRecord> collection)
        => collection.Database.DropCollectionAsync(collection.CollectionNamespace.CollectionName);
}