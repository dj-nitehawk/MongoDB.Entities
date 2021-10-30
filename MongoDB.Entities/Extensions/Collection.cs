using MongoDB.Driver;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public static partial class Extensions
    {
        /// <summary>
        /// Gets the IMongoCollection for a given IEntity type.
        /// <para>TIP: Try never to use this unless really neccessary.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static IMongoCollection<T> Collection<T>(this T _, string tenantPrefix) where T : IEntity => DB.Collection<T>(tenantPrefix);

        /// <summary>
        /// Gets the collection name for this entity
        /// </summary>
        public static string CollectionName<T>(this T _) where T : IEntity
        {
            return DB.CollectionName<T>();
        }

        /// <summary>
        /// Drops a join collection
        /// </summary>
        /// <param name="collection"></param>
        public static Task DropAsync(this IMongoCollection<JoinRecord> collection)
        {
            return collection.Database.DropCollectionAsync(collection.CollectionNamespace.CollectionName);
        }
    }
}
