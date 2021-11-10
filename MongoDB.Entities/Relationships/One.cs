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
    public class One<T> where T : IEntity
    {
        private T? _cache;

        /// <summary>
        /// The Id of the entity referenced by this instance.
        /// </summary>
        [AsObjectId]
        public string? ID { get; set; }

        public T? Cache
        {
            get => _cache;
            set
            {
                value?.ThrowIfUnsaved();
                _cache = value;
                if (ID != value?.ID)
                {
                    ID = value?.ID!;
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
        public static implicit operator One<T>(string id)
        {
            return new One<T>() { ID = id };
        }

        /// <summary>
        /// Operator for returning a new One&lt;T&gt; object from an entity
        /// </summary>
        /// <param name="entity">The entity to make a reference to</param>
        public static implicit operator One<T>(T entity)
        {
            return new One<T>(entity);
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

            return Cache = await new Find<T>(context, collection ?? context.Collection<T>(collectionName)).OneAsync(ID, cancellation);
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
        public async Task<T?> ToEntityAsync<TFrom>(DBContext context, Expression<Func<TFrom, T>> projection, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<TFrom>? collection = null) where TFrom : IEntity
        {
            if (ID is null)
            {
                return default;
            }

            return Cache = (await new Find<TFrom, T>(context, collection ?? context.Collection<TFrom>(collectionName))
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
        public async Task<T?> ToEntityAsync<TFrom>(DBContext context, Func<ProjectionDefinitionBuilder<TFrom>, ProjectionDefinition<TFrom, T>> projection, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<TFrom>? collection = null) where TFrom : IEntity
        {
            if (ID is null)
            {
                return default;
            }
            return Cache = (await new Find<TFrom, T>(context, collection ?? context.Collection<TFrom>(collectionName))
                        .Match(ID)
                        .Project(projection)
                        .ExecuteAsync(cancellation).ConfigureAwait(false))
                   .SingleOrDefault();
        }
    }
}
