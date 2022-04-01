using MongoDB.Driver;

namespace MongoDB.Entities
{
    public partial class DBContext
    {
        /// <summary>
        /// Starts a replace command for the given entity type
        /// <para>TIP: Only the first matched entity will be replaced</para>
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        public Replace<T, string> Replace<T>(string? collectionName = null, IMongoCollection<T>? collection = null)
            where T : IEntity => Replace<T, string>(collectionName, collection);

        /// <summary>
        /// Starts a replace command for the given entity type
        /// <para>TIP: Only the first matched entity will be replaced</para>
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TId">ID type</typeparam>
        public Replace<T, TId> Replace<T, TId>(string? collectionName = null, IMongoCollection<T>? collection = null)
        where TId : IComparable<TId>, IEquatable<TId>
        where T : IEntity<TId>
        {
            ThrowIfModifiedByIsEmpty<T>();
            return new Replace<T, TId>(this, Collection(collectionName, collection), OnBeforeSave<T>);
        }
    }
}
