using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public static partial class Extensions
    {
        /// <summary>
        /// Saves a complete entity replacing an existing entity or creating a new one if it does not exist. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public static Task SaveAsync<T>(this T entity, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
            where T : IEntity
        {
            return DB.Context.SaveAsync(entity, cancellation: cancellation, collectionName: collectionName, collection: collection);
        }

        /// <summary>
        /// Saves a batch of complete entities replacing existing ones or creating new ones if they do not exist. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public static Task<BulkWriteResult<T>> SaveAsync<T>(this IEnumerable<T> entities, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
            where T : IEntity
        {
            return DB.Context.SaveAsync(entities: entities, cancellation, collectionName: collectionName, collection: collection);
        }

        /// <summary>
        /// Saves an entity partially with only the specified subset of properties. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>TIP: The properties to be saved can be specified with a 'New' expression. 
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static Task<UpdateResult> SaveOnlyAsync<T>(this T entity, Expression<Func<T, object>> members, IClientSessionHandle session = null, CancellationToken cancellation = default, string tenantPrefix = null) where T : IEntity
        {
            return DB.SaveOnlyAsync(entity, members, session, cancellation, tenantPrefix);
        }

        /// <summary>
        /// Saves a batch of entities partially with only the specified subset of properties. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>TIP: The properties to be saved can be specified with a 'New' expression. 
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The batch of entities to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static Task<BulkWriteResult<T>> SaveOnlyAsync<T>(this IEnumerable<T> entities, Expression<Func<T, object>> members, IClientSessionHandle session = null, CancellationToken cancellation = default, string tenantPrefix = null) where T : IEntity
        {
            return DB.SaveOnlyAsync(entities, members, session, cancellation, tenantPrefix);
        }

        /// <summary>
        /// Saves an entity partially excluding the specified subset of properties. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>TIP: The properties to be excluded can be specified with a 'New' expression. 
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static Task<UpdateResult> SaveExceptAsync<T>(this T entity, Expression<Func<T, object>> members, IClientSessionHandle session = null, CancellationToken cancellation = default, string tenantPrefix = null) where T : IEntity
        {
            return DB.SaveExceptAsync(entity, members, session, cancellation, tenantPrefix);
        }

        /// <summary>
        /// Saves a batch of entities partially excluding the specified subset of properties. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>TIP: The properties to be excluded can be specified with a 'New' expression. 
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The batch of entities to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static Task<BulkWriteResult<T>> SaveExceptAsync<T>(this IEnumerable<T> entities, Expression<Func<T, object>> members, IClientSessionHandle session = null, CancellationToken cancellation = default, string tenantPrefix = null) where T : IEntity
        {
            return DB.SaveExceptAsync(entities, members, session, cancellation, tenantPrefix);
        }

        /// <summary>
        /// Saves an entity partially while excluding some properties. 
        /// The properties to be excluded can be specified using the [Preserve] attribute.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="session">Optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static Task<UpdateResult> SavePreservingAsync<T>(this T entity, IClientSessionHandle session = null, CancellationToken cancellation = default, string tenantPrefix = null) where T : IEntity
        {
            return DB.SavePreservingAsync(entity, session, cancellation, tenantPrefix);
        }
    }
}
