using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <summary>
    /// Saves a complete entity replacing an existing entity or creating a new one if it does not exist.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="dbInstance">The DBInstance to use for this operation</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public static Task SaveAsync<T>(this T entity, DBInstance? dbInstance = null, IClientSessionHandle? session = null, CancellationToken cancellation = default) where T : IEntity
        => DBInstance.InstanceOrDefault(dbInstance).SaveAsync(entity, session, cancellation);

    /// <summary>
    /// Saves a batch of complete entities replacing existing ones or creating new ones if they do not exist.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="dbInstance">The DBInstance to use for this operation</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public static Task<BulkWriteResult<T>> SaveAsync<T>(this IEnumerable<T> entities,
                                                        DBInstance? dbInstance = null,
                                                        IClientSessionHandle? session = null,
                                                        CancellationToken cancellation = default)
        where T : IEntity
        => DBInstance.InstanceOrDefault(dbInstance).SaveAsync(entities, session, cancellation);

    /// <summary>
    /// Saves an entity partially with only the specified subset of properties.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>
    /// TIP: The properties to be saved can be specified with a 'New' expression.
    /// You can only specify root level properties with the expression.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The entity to save</param>
    /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
    /// <param name="dbInstance">The DBInstance to use for this operation</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public static Task<UpdateResult> SaveOnlyAsync<T>(this T entity,
                                                      Expression<Func<T, object?>> members,
                                                      DBInstance? dbInstance = null,
                                                      IClientSessionHandle? session = null,
                                                      CancellationToken cancellation = default) where T : IEntity
        => DBInstance.InstanceOrDefault(dbInstance).SaveOnlyAsync(entity, members, session, cancellation);

    /// <summary>
    /// Saves an entity partially with only the specified subset of properties.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>
    /// TIP: The properties to be saved can be specified with an IEnumerable.
    /// Property names must match exactly.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The entity to save</param>
    /// <param name="propNames">new List { "PropOne", "PropTwo" }</param>
    /// <param name="dbInstance">The DBInstance to use for this operation</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public static Task<UpdateResult> SaveOnlyAsync<T>(this T entity,
                                                      IEnumerable<string> propNames,
                                                      DBInstance? dbInstance = null,
                                                      IClientSessionHandle? session = null,
                                                      CancellationToken cancellation = default) where T : IEntity
        => DBInstance.InstanceOrDefault(dbInstance).SaveOnlyAsync(entity, propNames, session, cancellation);

    /// <summary>
    /// Saves a batch of entities partially with only the specified subset of properties.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>
    /// TIP: The properties to be saved can be specified with a 'New' expression.
    /// You can only specify root level properties with the expression.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entities">The batch of entities to save</param>
    /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
    /// <param name="dbInstance">The DBInstance to use for this operation</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public static Task<BulkWriteResult<T>> SaveOnlyAsync<T>(this IEnumerable<T> entities,
                                                            Expression<Func<T, object?>> members,
                                                            DBInstance? dbInstance = null,
                                                            IClientSessionHandle? session = null,
                                                            CancellationToken cancellation = default) where T : IEntity
        => DBInstance.InstanceOrDefault(dbInstance).SaveOnlyAsync(entities, members, session, cancellation);

    /// <summary>
    /// Saves a batch of entities partially with only the specified subset of properties.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>
    /// TIP: The properties to be saved can be specified with an IEnumerable.
    /// Property names must match exactly.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entities">The batch of entities to save</param>
    /// <param name="propNames">new List { "PropOne", "PropTwo" }</param>
    /// <param name="dbInstance">The DBInstance to use for this operation</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public static Task<BulkWriteResult<T>> SaveOnlyAsync<T>(this IEnumerable<T> entities,
                                                            IEnumerable<string> propNames,
                                                            DBInstance? dbInstance = null, 
                                                            IClientSessionHandle? session = null,
                                                            CancellationToken cancellation = default) where T : IEntity
        => DBInstance.InstanceOrDefault(dbInstance).SaveOnlyAsync(entities, propNames, session, cancellation);

    /// <summary>
    /// Saves an entity partially excluding the specified subset of properties.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>
    /// TIP: The properties to be excluded can be specified with a 'New' expression.
    /// You can only specify root level properties with the expression.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The entity to save</param>
    /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
    /// <param name="dbInstance">The DBInstance to use for this operation</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public static Task<UpdateResult> SaveExceptAsync<T>(this T entity,
                                                        Expression<Func<T, object?>> members,
                                                        DBInstance? dbInstance = null, 
                                                        IClientSessionHandle? session = null,
                                                        CancellationToken cancellation = default) where T : IEntity
        => DBInstance.InstanceOrDefault(dbInstance).SaveExceptAsync(entity, members, session, cancellation);

    /// <summary>
    /// Saves an entity partially excluding the specified subset of properties.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>
    /// TIP: The properties to be saved can be specified with an IEnumerable.
    /// Property names must match exactly.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The entity to save</param>
    /// <param name="propNames">new List { "PropOne", "PropTwo" }</param>
    /// <param name="dbInstance">The DBInstance to use for this operation</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public static Task<UpdateResult> SaveExceptAsync<T>(this T entity,
                                                        IEnumerable<string> propNames,
                                                        DBInstance? dbInstance = null, 
                                                        IClientSessionHandle? session = null,
                                                        CancellationToken cancellation = default) where T : IEntity
        => DBInstance.InstanceOrDefault(dbInstance).SaveExceptAsync(entity, propNames, session, cancellation);

    /// <summary>
    /// Saves a batch of entities partially excluding the specified subset of properties.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>
    /// TIP: The properties to be excluded can be specified with a 'New' expression.
    /// You can only specify root level properties with the expression.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entities">The batch of entities to save</param>
    /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
    /// <param name="dbInstance">The DBInstance to use for this operation</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public static Task<BulkWriteResult<T>> SaveExceptAsync<T>(this IEnumerable<T> entities,
                                                              Expression<Func<T, object?>> members,
                                                              DBInstance? dbInstance = null,
                                                              IClientSessionHandle? session = null,
                                                              CancellationToken cancellation = default) where T : IEntity
        => DBInstance.InstanceOrDefault(dbInstance).SaveExceptAsync(entities, members, session, cancellation);

    /// <summary>
    /// Saves a batch of entities partially excluding the specified subset of properties.
    /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
    /// <para>
    /// TIP: The properties to be saved can be specified with an IEnumerable.
    /// Property names must match exactly.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entities">The batch of entities to save</param>
    /// <param name="propNames">new List { "PropOne", "PropTwo" }</param>
    /// <param name="dbInstance">The DBInstance to use for this operation</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public static Task<BulkWriteResult<T>> SaveExceptAsync<T>(this IEnumerable<T> entities,
                                                              IEnumerable<string> propNames,
                                                              DBInstance? dbInstance = null,
                                                              IClientSessionHandle? session = null,
                                                              CancellationToken cancellation = default) where T : IEntity
        => DBInstance.InstanceOrDefault(dbInstance).SaveExceptAsync(entities, propNames, session, cancellation);

    /// <summary>
    /// Saves an entity partially while excluding some properties.
    /// The properties to be excluded can be specified using the [Preserve] attribute.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="entity">The entity to save</param>
    /// <param name="dbInstance">The DBInstance to use for this operation</param>
    /// <param name="session"></param>
    /// <param name="cancellation">An optional cancellation token</param>
    public static Task<UpdateResult> SavePreservingAsync<T>(this T entity, DBInstance? dbInstance = null, IClientSessionHandle? session = null, CancellationToken cancellation = default)
        where T : IEntity
        => DBInstance.InstanceOrDefault(dbInstance).SavePreservingAsync(entity, session, cancellation);
}