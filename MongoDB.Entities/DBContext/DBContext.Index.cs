using MongoDB.Driver;

namespace MongoDB.Entities
{
    public partial class DBContext
    {
        /// <summary>
        /// Represents an index for a given IEntity
        /// <para>TIP: Define the keys first with .Key() method and finally call the .Create() method.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TId">ID type</typeparam>
        public Index<T, TId> Index<T, TId>(string? collectionName = null, IMongoCollection<T>? collection = null)
            where TId : IComparable<TId>, IEquatable<TId>
            where T : IEntity<TId>
        {
            return new Index<T, TId>(this, Collection(collectionName, collection));
        }
    }
}
