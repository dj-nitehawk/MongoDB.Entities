using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public static partial class DB
    {


        /// <summary>
        /// Saves a complete entity replacing an existing entity or creating a new one if it does not exist. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="cancellation">And optional cancellation token</param>
        public static Task SaveAsync<T>(T entity, CancellationToken cancellation = default) where T : IEntity
        {
            return Context.SaveAsync<T>(entity, cancellation);
        }

        /// <summary>
        /// Saves a batch of complete entities replacing existing ones or creating new ones if they do not exist. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="cancellation">And optional cancellation token</param>
        public static Task<BulkWriteResult<T>> SaveAsync<T>(IEnumerable<T> entities, CancellationToken cancellation = default) where T : IEntity
        {
            return Context.SaveAsync(entities, cancellation);
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
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<UpdateResult> SaveOnlyAsync<T>(T entity, Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntity
        {
            return Context.SaveOnlyAsync(entity, members, cancellation);

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
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<BulkWriteResult<T>> SaveOnlyAsync<T>(IEnumerable<T> entities, Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntity
        {
            return Context.SaveOnlyAsync(entities, members, cancellation);
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
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<UpdateResult> SaveExceptAsync<T>(T entity, Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntity
        {
            return Context.SaveExceptAsync(entity, members, cancellation);
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
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<BulkWriteResult<T>> SaveExceptAsync<T>(IEnumerable<T> entities, Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntity
        {
            return Context.SaveExceptAsync(entities, members, cancellation);
        }

        /// <summary>
        /// Saves an entity partially while excluding some properties. 
        /// The properties to be excluded can be specified using the [Preserve] or [DontPreserve] attributes.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<UpdateResult> SavePreservingAsync<T>(T entity, CancellationToken cancellation = default) where T : IEntity
        {
            return Context.SavePreservingAsync(entity, cancellation);
        }


    }
}
