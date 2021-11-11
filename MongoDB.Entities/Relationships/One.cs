using MongoDB.Driver;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents a one-to-one relationship with an IEntity.
    /// </summary>
    /// <typeparam name="T">Any type that implements IEntity</typeparam>
    /// <typeparam name="TId">Any type that implements IEntity</typeparam>
    public class One<T, TId>
        where TId : IComparable<TId>, IEquatable<TId>
        where T : IEntity<TId>
    {
        private T? _cache;

        /// <summary>
        /// The Id of the entity referenced by this instance.
        /// </summary>
        [AsObjectId]
        public TId? ID { get; set; }

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
        /// Operator for returning a new One&lt;T&gt; object from a string ID
        /// </summary>
        /// <param name="id">The ID to create a new One&lt;T&gt; with</param>
        public static implicit operator One<T, TId>(TId id)
        {
            return new One<T, TId>() { ID = id };
        }

        /// <summary>
        /// Operator for returning a new One&lt;T&gt; object from an entity
        /// </summary>
        /// <param name="entity">The entity to make a reference to</param>
        public static implicit operator One<T, TId>(T entity)
        {
            return new One<T, TId>(entity);
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        /// <returns>A Task containing the actual entity</returns>
        public async Task<T?> ToEntityAsync(DBContext context, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
        {
            if (ID is null)
            {
                return default;
            }

            return Cache = await new Find<T, TId>(context, collection ?? context.Collection<T>(collectionName)).OneAsync(ID, cancellation);
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database with a projection.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projection">x => new Test { PropName = x.Prop }</param>
        /// <param name = "cancellation" > An optional cancellation token</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        /// <returns>A Task containing the actual projected entity</returns>
        public async Task<TTo?> ToEntityAsync<TTo>(DBContext context, Expression<Func<T, TTo>> projection, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
        {
            if (ID is null)
            {
                return default;
            }

            return (await new Find<T, TId, TTo>(context, collection ?? context.Collection<T>(collectionName))
                        .Match(ID)
                        .Project(projection)
                        .ExecuteAsync(cancellation).ConfigureAwait(false))
                   .SingleOrDefault();
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database with a projection.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projection">p=> p.Include("Prop1").Exclude("Prop2")</param>
        /// <param name = "cancellation" > An optional cancellation token</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        /// <returns>A Task containing the actual projected entity</returns>
        public async Task<TTo?> ToEntityAsync<TTo>(DBContext context, Func<ProjectionDefinitionBuilder<T>, ProjectionDefinition<T, TTo>> projection, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
        {
            if (ID is null)
            {
                return default;
            }
            return (await new Find<T, TId, TTo>(context, collection ?? context.Collection<T>(collectionName))
                        .Match(ID)
                        .Project(projection)
                        .ExecuteAsync(cancellation).ConfigureAwait(false))
                   .SingleOrDefault();
        }
    }

    public class One<T> : One<T, string> where T : IEntity
    {

    }
}
