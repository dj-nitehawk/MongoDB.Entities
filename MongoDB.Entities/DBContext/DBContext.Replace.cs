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
        public Replace<T> Replace<T>(string? collectionName = null, IMongoCollection<T>? collection = null) where T : IEntity
        {
            ThrowIfModifiedByIsEmpty<T>();
            return new Replace<T>(this, Collection(collectionName, collection), OnBeforeSave<T>);
        }
    }
}
