using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

/// <summary>
/// Represents a one-to-one relationship with an IEntity.
/// </summary>
/// <typeparam name="TEntity">Any type that implements IEntity</typeparam>
public class One<TEntity> where TEntity : IEntity
{
    /// <summary>
    /// The Id of the entity referenced by this instance.
    /// </summary>
    [AsObjectId]
    public string ID { get; set; } = default!;

    public One() { }

    /// <summary>
    /// Initializes a reference to an entity in MongoDB.
    /// </summary>
    /// <param name="entity">The actual entity this reference represents.</param>
    public One(TEntity entity)
    {
        entity.ThrowIfUnsaved();
        ID = (string)entity.GetId();
    }

    /// <summary>
    /// Initializes a reference to an entity in MongoDB.
    /// </summary>
    /// <param name="id">the ID of the referenced entity</param>
    public One(string id)
    {
        ID = id;
    }

    /// <summary>
    /// Fetches the actual entity this reference represents from the database.
    /// </summary>
    /// <param name="session">An optional session</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <returns>A Task containing the actual entity</returns>
    public Task<TEntity> ToEntityAsync(IClientSessionHandle? session = null, CancellationToken cancellation = default)
        => new Find<TEntity>(session, null, Cache<TEntity>.DbInstance).OneAsync(ID, cancellation)!;

    /// <summary>
    /// Fetches the actual entity this reference represents from the database with a projection.
    /// </summary>
    /// <param name="projection">x => new Test { PropName = x.Prop }</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation"> An optional cancellation token</param>
    /// <exception cref="InvalidOperationException">
    /// thrown if the entity cannot be found in the database or more than one entity matching the ID is found.
    /// </exception>
    /// <returns>A Task containing the actual projected entity</returns>
    public async Task<TEntity> ToEntityAsync(Expression<Func<TEntity, TEntity>> projection,
                                             IClientSessionHandle? session = null,
                                             CancellationToken cancellation = default)
        => (await new Find<TEntity>(session, null, Cache<TEntity>.DbInstance)
                  .MatchID(ID)
                  .Project(projection)
                  .ExecuteAsync(cancellation)
                  .ConfigureAwait(false)).Single();

    /// <summary>
    /// Fetches the actual entity this reference represents from the database with a projection.
    /// </summary>
    /// <param name="projection">p=> p.Include("Prop1").Exclude("Prop2")</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation"> An optional cancellation token</param>
    /// <exception cref="InvalidOperationException">
    /// thrown if the entity cannot be found in the database or more than one entity matching the ID is found.
    /// </exception>
    /// <returns>A Task containing the actual projected entity</returns>
    public async Task<TEntity> ToEntityAsync(Func<ProjectionDefinitionBuilder<TEntity>, ProjectionDefinition<TEntity, TEntity>> projection,
                                             IClientSessionHandle? session = null,
                                             CancellationToken cancellation = default)
        => (await new Find<TEntity>(session, null, Cache<TEntity>.DbInstance)
                  .MatchID(ID)
                  .Project(projection)
                  .ExecuteAsync(cancellation).ConfigureAwait(false)).Single();
}

/// <summary>
/// Represents a one-to-one relationship with an IEntity.
/// </summary>
/// <typeparam name="TEntity">Any type that implements IEntity</typeparam>
/// <typeparam name="TIdentity">The type of the <see cref="ID" /> property</typeparam>
public class One<TEntity, TIdentity> where TEntity : IEntity where TIdentity : notnull
{
    /// <summary>
    /// The Id of the entity referenced by this instance.
    /// </summary>
    public TIdentity ID { get; set; } = default!;

    public One() { }

    /// <summary>
    /// Initializes a reference to an entity in MongoDB.
    /// </summary>
    /// <param name="entity">The actual entity this reference represents.</param>
    public One(TEntity entity)
    {
        entity.ThrowIfUnsaved();
        ID = (TIdentity)entity.GetId();
    }

    /// <summary>
    /// Initializes a reference to an entity in MongoDB.
    /// </summary>
    /// <param name="id">the ID of the referenced entity</param>
    public One(TIdentity id)
    {
        ID = id;
    }

    /// <summary>
    /// Operator for returning a new One&lt;T&gt; object from a object ID
    /// </summary>
    /// <param name="id">The ID to create a new One&lt;T&gt; with</param>
    public static One<TEntity, TIdentity> FromObject(TIdentity id)
        => new() { ID = id };

    /// <summary>
    /// Fetches the actual entity this reference represents from the database.
    /// </summary>
    /// <param name="session">An optional session</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <returns>A Task containing the actual entity</returns>
    public Task<TEntity> ToEntityAsync(IClientSessionHandle? session = null, CancellationToken cancellation = default)
        => new Find<TEntity>(session, null, Cache<TEntity>.DbInstance).OneAsync(ID, cancellation)!;

    /// <summary>
    /// Fetches the actual entity this reference represents from the database with a projection.
    /// </summary>
    /// <param name="projection">x => new Test { PropName = x.Prop }</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation"> An optional cancellation token</param>
    /// <exception cref="InvalidOperationException">
    /// thrown if the entity cannot be found in the database or more than one
    /// entity matching the ID is found.
    /// </exception>
    /// <returns>A Task containing the actual projected entity</returns>
    public async Task<TEntity> ToEntityAsync(Expression<Func<TEntity, TEntity>> projection,
                                             IClientSessionHandle? session = null,
                                             CancellationToken cancellation = default)
        => (await new Find<TEntity>(session, null, Cache<TEntity>.DbInstance)
                  .Match(ID)
                  .Project(projection)
                  .ExecuteAsync(cancellation)
                  .ConfigureAwait(false)).Single();

    /// <summary>
    /// Fetches the actual entity this reference represents from the database with a projection.
    /// </summary>
    /// <param name="projection">p=> p.Include("Prop1").Exclude("Prop2")</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation"> An optional cancellation token</param>
    /// <exception cref="InvalidOperationException">
    /// thrown if the entity cannot be found in the database or more than one
    /// entity matching the ID is found.
    /// </exception>
    /// <returns>A Task containing the actual projected entity</returns>
    public async Task<TEntity> ToEntityAsync(Func<ProjectionDefinitionBuilder<TEntity>, ProjectionDefinition<TEntity, TEntity>> projection,
                                             IClientSessionHandle? session = null,
                                             CancellationToken cancellation = default)
        => (await new Find<TEntity>(session, null, Cache<TEntity>.DbInstance)
                  .Match(ID)
                  .Project(projection)
                  .ExecuteAsync(cancellation).ConfigureAwait(false)).Single();
}