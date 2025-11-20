using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <param name="entity">The entity to save</param>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    extension<T>(T entity) where T : IEntity
    {
        /// <summary>
        /// Saves a complete entity replacing an existing entity or creating a new one if it does not exist.
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
        /// </summary>
        /// <param name="db">The DB instance to use for this operation</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task SaveAsync(DB? db = null, IClientSessionHandle? session = null, CancellationToken cancellation = default)
            => DB.InstanceOrDefault(db).SaveAsync(entity, session, cancellation);

        /// <summary>
        /// Saves an entity partially with only the specified subset of properties.
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>
        /// TIP: The properties to be saved can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.
        /// </para>
        /// </summary>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="db">The DB instance to use for this operation</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<UpdateResult> SaveOnlyAsync(Expression<Func<T, object?>> members,
                                                DB? db = null,
                                                IClientSessionHandle? session = null,
                                                CancellationToken cancellation = default)
            => DB.InstanceOrDefault(db).SaveOnlyAsync(entity, members, session, cancellation);

        /// <summary>
        /// Saves an entity partially with only the specified subset of properties.
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>
        /// TIP: The properties to be saved can be specified with an IEnumerable.
        /// Property names must match exactly.
        /// </para>
        /// </summary>
        /// <param name="propNames">new List { "PropOne", "PropTwo" }</param>
        /// <param name="db">The DB instance to use for this operation</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<UpdateResult> SaveOnlyAsync(IEnumerable<string> propNames,
                                                DB? db = null,
                                                IClientSessionHandle? session = null,
                                                CancellationToken cancellation = default)
            => DB.InstanceOrDefault(db).SaveOnlyAsync(entity, propNames, session, cancellation);

        /// <summary>
        /// Saves an entity partially excluding the specified subset of properties.
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>
        /// TIP: The properties to be excluded can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.
        /// </para>
        /// </summary>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="db">The DB instance to use for this operation</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<UpdateResult> SaveExceptAsync(Expression<Func<T, object?>> members,
                                                  DB? db = null,
                                                  IClientSessionHandle? session = null,
                                                  CancellationToken cancellation = default)
            => DB.InstanceOrDefault(db).SaveExceptAsync(entity, members, session, cancellation);

        /// <summary>
        /// Saves an entity partially excluding the specified subset of properties.
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>
        /// TIP: The properties to be saved can be specified with an IEnumerable.
        /// Property names must match exactly.
        /// </para>
        /// </summary>
        /// <param name="propNames">new List { "PropOne", "PropTwo" }</param>
        /// <param name="db">The DB instance to use for this operation</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<UpdateResult> SaveExceptAsync(IEnumerable<string> propNames,
                                                  DB? db = null,
                                                  IClientSessionHandle? session = null,
                                                  CancellationToken cancellation = default)
            => DB.InstanceOrDefault(db).SaveExceptAsync(entity, propNames, session, cancellation);

        /// <summary>
        /// Saves an entity partially while excluding some properties.
        /// The properties to be excluded can be specified using the [Preserve] attribute.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="db">The DB instance to use for this operation</param>
        /// <param name="session"></param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<UpdateResult> SavePreservingAsync(DB? db = null,
                                                      IClientSessionHandle? session = null,
                                                      CancellationToken cancellation = default)
            => DB.InstanceOrDefault(db).SavePreservingAsync(entity, session, cancellation);
    }

    /// <param name="entities">The batch of entities to save</param>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    extension<T>(IEnumerable<T> entities) where T : IEntity
    {
        /// <summary>
        /// Saves a batch of complete entities replacing existing ones or creating new ones if they do not exist.
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
        /// </summary>
        /// <param name="db">The DB instance to use for this operation</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<BulkWriteResult<T>> SaveAsync(DB? db = null,
                                                  IClientSessionHandle? session = null,
                                                  CancellationToken cancellation = default)
            => DB.InstanceOrDefault(db).SaveAsync(entities, session, cancellation);

        /// <summary>
        /// Saves a batch of entities partially with only the specified subset of properties.
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>
        /// TIP: The properties to be saved can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.
        /// </para>
        /// </summary>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="db">The DB instance to use for this operation</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<BulkWriteResult<T>> SaveOnlyAsync(Expression<Func<T, object?>> members,
                                                      DB? db = null,
                                                      IClientSessionHandle? session = null,
                                                      CancellationToken cancellation = default)
            => DB.InstanceOrDefault(db).SaveOnlyAsync(entities, members, session, cancellation);

        /// <summary>
        /// Saves a batch of entities partially with only the specified subset of properties.
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>
        /// TIP: The properties to be saved can be specified with an IEnumerable.
        /// Property names must match exactly.
        /// </para>
        /// </summary>
        /// <param name="propNames">new List { "PropOne", "PropTwo" }</param>
        /// <param name="db">The DB instance to use for this operation</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<BulkWriteResult<T>> SaveOnlyAsync(IEnumerable<string> propNames,
                                                      DB? db = null,
                                                      IClientSessionHandle? session = null,
                                                      CancellationToken cancellation = default)
            => DB.InstanceOrDefault(db).SaveOnlyAsync(entities, propNames, session, cancellation);

        /// <summary>
        /// Saves a batch of entities partially excluding the specified subset of properties.
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>
        /// TIP: The properties to be excluded can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.
        /// </para>
        /// </summary>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="db">The DB instance to use for this operation</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<BulkWriteResult<T>> SaveExceptAsync(Expression<Func<T, object?>> members,
                                                        DB? db = null,
                                                        IClientSessionHandle? session = null,
                                                        CancellationToken cancellation = default)
            => DB.InstanceOrDefault(db).SaveExceptAsync(entities, members, session, cancellation);

        /// <summary>
        /// Saves a batch of entities partially excluding the specified subset of properties.
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>
        /// TIP: The properties to be saved can be specified with an IEnumerable.
        /// Property names must match exactly.
        /// </para>
        /// </summary>
        /// <param name="propNames">new List { "PropOne", "PropTwo" }</param>
        /// <param name="db">The DB instance to use for this operation</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<BulkWriteResult<T>> SaveExceptAsync(IEnumerable<string> propNames,
                                                        DB? db = null,
                                                        IClientSessionHandle? session = null,
                                                        CancellationToken cancellation = default)
            => DB.InstanceOrDefault(db).SaveExceptAsync(entities, propNames, session, cancellation);
    }
}