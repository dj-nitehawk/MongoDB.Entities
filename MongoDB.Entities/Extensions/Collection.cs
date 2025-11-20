using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    extension<T>(T _) where T : IEntity
    {
        /// <summary>
        /// Gets the IMongoCollection for a given IEntity type.
        /// <para>TIP: Try never to use this unless really necessary.</para>
        /// </summary>
        /// <param name="db">The DB instance to use for this operation</param>
        public IMongoCollection<T> Collection(DB? db = null)
            => DB.InstanceOrDefault(db).Collection<T>();

        /// <summary>
        /// Gets the collection name for this entity
        /// </summary>
        /// <param name="db">The DB instance to use for this operation</param>
        public string CollectionName(DB? db = null)
            => DB.InstanceOrDefault(db).CollectionName<T>();
    }

    /// <summary>
    /// Drops a join collection
    /// </summary>
    /// <param name="collection"></param>
    public static Task DropAsync(this IMongoCollection<JoinRecord> collection)
        => collection.Database.DropCollectionAsync(collection.CollectionNamespace.CollectionName);
}