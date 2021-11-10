using MongoDB.Driver;
using System.Reflection;

namespace MongoDB.Entities
{
    public partial class DBContext
    {
        /// <summary>
        /// Starts an update command for the given entity type
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        public Update<T> Update<T>(string? collectionName = null, IMongoCollection<T>? collection = null) where T : IEntity
        {
            var cmd = new Update<T>(this, Collection(collectionName, collection), OnBeforeUpdate<T, Update<T>>);
            if (Cache<T>().ModifiedByProp is PropertyInfo ModifiedByProp)
            {
                ThrowIfModifiedByIsEmpty<T>();
                cmd.Modify(b => b.Set(ModifiedByProp.Name, ModifiedBy));
            }
            return cmd;
        }

        /// <summary>
        /// Starts an update-and-get command for the given entity type
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        public UpdateAndGet<T, T> UpdateAndGet<T>(string? collectionName = null, IMongoCollection<T>? collection = null) where T : IEntity
        {
            return UpdateAndGet<T, T>(collectionName, collection);
        }

        /// <summary>
        /// Starts an update-and-get command with projection support for the given entity type
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TProjection">The type of the end result</typeparam>
        public UpdateAndGet<T, TProjection> UpdateAndGet<T, TProjection>(string? collectionName = null, IMongoCollection<T>? collection = null) where T : IEntity
        {
            var cmd = new UpdateAndGet<T, TProjection>(this, Collection(collectionName, collection), OnBeforeUpdate<T, UpdateAndGet<T, TProjection>>);
            if (Cache<T>().ModifiedByProp is PropertyInfo ModifiedByProp)
            {
                ThrowIfModifiedByIsEmpty<T>();
                cmd.Modify(b => b.Set(ModifiedByProp.Name, ModifiedBy));
            }
            return cmd;
        }
    }
}
