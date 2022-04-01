using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public static partial class Extensions
    {
        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="ignoreGlobalFilters"></param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public static Task<DeleteResult> DeleteAsync<T, TId>(this T entity, CancellationToken cancellation = default, bool ignoreGlobalFilters = false, string? collectionName = null, IMongoCollection<T>? collection = null)
            where TId : IEquatable<TId>, IComparable<TId>
            where T : IEntity<TId>
        {
            entity.ThrowIfUnsaved();
            return DB.Context.DeleteAsync(entity.ID!, cancellation, ignoreGlobalFilters: ignoreGlobalFilters, collectionName: collectionName, collection: collection);
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="ignoreGlobalFilters"></param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public static Task<DeleteResult> DeleteAsync<T>(this T entity, CancellationToken cancellation = default, bool ignoreGlobalFilters = false, string? collectionName = null, IMongoCollection<T>? collection = null)
            where T : IEntity
        {
            return DeleteAsync<T, string>(entity, cancellation, ignoreGlobalFilters: ignoreGlobalFilters, collectionName: collectionName, collection: collection);
        }


        /// <summary>
        /// Deletes multiple entities from the database
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <param name="entities"></param>        
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="ignoreGlobalFilters"></param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>        
        public static Task<DeleteResult> DeleteAllAsync<T>(this IEnumerable<T> entities, CancellationToken cancellation = default, bool ignoreGlobalFilters = false, string? collectionName = null, IMongoCollection<T>? collection = null)
            where T : IEntity
        {
            return DeleteAllAsync<T, string>(entities, cancellation: cancellation, ignoreGlobalFilters: ignoreGlobalFilters, collectionName: collectionName, collection: collection);
        }

        /// <summary>
        /// Deletes multiple entities from the database
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <param name="entities"></param>        
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="ignoreGlobalFilters"></param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>        
        public static Task<DeleteResult> DeleteAllAsync<T, TId>(this IEnumerable<T> entities, CancellationToken cancellation = default, bool ignoreGlobalFilters = false, string? collectionName = null, IMongoCollection<T>? collection = null)
            where TId : IEquatable<TId>, IComparable<TId>
            where T : IEntity<TId>
        {
            return DB.Context.DeleteAsync<T, TId>(IDs: entities.Select(e => e.ID!), cancellation: cancellation, ignoreGlobalFilters: ignoreGlobalFilters, collectionName: collectionName, collection: collection);
        }

    }
}
