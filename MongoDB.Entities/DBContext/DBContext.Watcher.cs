using MongoDB.Driver;
using System.Collections.Generic;

namespace MongoDB.Entities
{
    public partial class DBContext
    {
        /// <summary>
        /// Retrieves the 'change-stream' watcher instance for a given unique name. 
        /// If an instance for the name does not exist, it will return a new instance. 
        /// If an instance already exists, that instance will be returned.
        /// </summary>
        /// <typeparam name="T">The entity type to get a watcher for</typeparam>
        /// <param name="name">A unique name for the watcher of this entity type. Names can be duplicate among different entity types.</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public Watcher<T> Watcher<T>(string name, string? collectionName = null, IMongoCollection<T>? collection = null) where T : IEntity
        {
            var cache = Cache<T>();
            if (cache.Watchers.TryGetValue(name.ToLowerInvariant().Trim(), out Watcher<T> watcher))
                return watcher;

            watcher = new Watcher<T>(name.ToLowerInvariant().Trim(), this, Collection(collectionName, collection));
            cache.Watchers.TryAdd(name, watcher);
            return watcher;
        }

        /// <summary>
        /// Returns all the watchers for a given entity type
        /// </summary>
        /// <typeparam name="T">The entity type to get the watcher of</typeparam>
        public IEnumerable<Watcher<T>> Watchers<T>() where T : IEntity => Cache<T>().Watchers.Values;
    }
}
