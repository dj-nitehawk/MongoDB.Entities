using MongoDB.Driver;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities;
public interface IOneRelation<T>
{
    T? Cache { get; set; }
}
/// <summary>
/// Represents a one-to-x relationship.
/// Note: this doesn't get serialized nor store any information, it just marks a related entity
/// </summary>
/// <typeparam name="T">Any type</typeparam>
public class One<T> : IOneRelation<T>
{
    [BsonIgnore]
    public T? Cache { get; set; }

    public One()
    {
    }

    /// <summary>
    /// Initializes a reference to an entity in MongoDB.
    /// </summary>
    /// <param name="entity">The actual entity this reference represents.</param>
    internal One(T entity)
    {
        Cache = entity;
    }


    /// <summary>
    /// Operator for returning a new MarkerOne&lt;T&gt; object from an entity
    /// </summary>
    /// <param name="entity">The entity to make a reference to</param>
    public static implicit operator One<T>(T entity)
    {
        return new One<T>(entity);
    }

    /// <summary>
    /// Returns a find for the related entity
    /// </summary>
    /// <param name="context"></param>
    /// <param name="filter"></param>
    /// <param name="collectionName"></param>
    /// <param name="collection"></param>
    /// <returns></returns>
    public Find<T, T> ToFind(DBContext context, FilterDefinition<T> filter, string? collectionName = null, IMongoCollection<T>? collection = null)
    {
        return context.Find(collectionName, collection).Match(filter);
    }

    ///// <summary>
    ///// Fetches the actual entity this reference represents from the database.
    ///// </summary>
    ///// <param name="context"></param>
    ///// <param name="cancellation">An optional cancellation token</param>
    ///// <param name="collectionName"></param>
    ///// <param name="collection"></param>
    ///// <returns>A Task containing the actual entity</returns>
    //public async Task<T?> ToEntityAsync(DBContext context, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
    //{
    //    if (ID is null)
    //    {
    //        return default;
    //    }

    //    return Cache = context.Find(collectionName, collection).OneAsync(ID, cancellation);
    //}

    ///// <summary>
    ///// Fetches the actual entity this reference represents from the database with a projection.
    ///// </summary>
    ///// <param name="context"></param>
    ///// <param name="projection">x => new Test { PropName = x.Prop }</param>
    ///// <param name = "cancellation" > An optional cancellation token</param>
    ///// <param name="collectionName"></param>
    ///// <param name="collection"></param>
    ///// <returns>A Task containing the actual projected entity</returns>
    //public async Task<TTo?> ToEntityAsync<TTo>(DBContext context, Expression<Func<T, TTo>> projection, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
    //{
    //    if (ID is null)
    //    {
    //        return default;
    //    }

    //    return (await context.Find<T, TTo>(collectionName, collection)
    //                .MatchID(ID)
    //                .Project(projection)
    //                .ExecuteAsync(cancellation).ConfigureAwait(false))
    //           .SingleOrDefault();
    //}

    ///// <summary>
    ///// Fetches the actual entity this reference represents from the database with a projection.
    ///// </summary>
    ///// <param name="context"></param>
    ///// <param name="projection">p=> p.Include("Prop1").Exclude("Prop2")</param>
    ///// <param name = "cancellation" > An optional cancellation token</param>
    ///// <param name="collectionName"></param>
    ///// <param name="collection"></param>
    ///// <returns>A Task containing the actual projected entity</returns>
    //public async Task<TTo?> ToEntityAsync<TTo>(DBContext context, Func<ProjectionDefinitionBuilder<T>, ProjectionDefinition<T, TTo>> projection, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
    //{
    //    if (ID is null)
    //    {
    //        return default;
    //    }
    //    return (await new Find<T, TId, TTo>(context, collection ?? context.Collection<T>(collectionName))
    //                .Match(ID)
    //                .Project(projection)
    //                .ExecuteAsync(cancellation).ConfigureAwait(false))
    //           .SingleOrDefault();
    //}
}

/// <summary>
/// Represents a one-to-x relationship with a known Id.
/// This DOES get serialized to
/// {
///     "ID" : "xxxxxxxx"
/// }
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="TId"></typeparam>
public class One<T, TId> : IOneRelation<T>
    where T : IEntity<TId>
    where TId : IComparable<TId>, IEquatable<TId>
{
    private T? _cache;

    [BsonIgnore]
    public T? Cache
    {
        get => _cache;
        set
        {
            value?.ThrowIfUnsaved();
            _cache = value;
            if (value is not null)
            {
                if (!EqualityComparer<TId?>.Default.Equals(ID, value.ID))
                {
                    ID = value.ID!;
                }
            }
            else
            {
                ID = default;
            }
        }
    }

    public TId? ID { get; set; }

    public One()
    {

    }
    public One(TId id)
    {
        ID = id;
    }
    public One(T entity)
    {
        entity.ThrowIfUnsaved();
        _cache = entity;
        ID = entity.ID;
    }
}