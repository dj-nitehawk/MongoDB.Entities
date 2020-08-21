using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
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
        /// <summary>
        /// The Id of the entity referenced by this instance.
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string ID { get; set; }

        public One()
        { }

        /// <summary>
        /// Initializes a reference to an entity in MongoDB. 
        /// </summary>
        /// <param name="entity">The actual entity this reference represents.</param>
        internal One(T entity)
        {
            entity.ThrowIfUnsaved();
            ID = entity.ID;
        }

        /// <summary>
        /// Operator for returning a new One&lt;T&gt; object from a string ID
        /// </summary>
        /// <param name="id">The ID to create a new One&lt;T&gt; with</param>
        public static implicit operator One<T>(string id)
        {
            return new One<T> { ID = id };
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
        /// <returns>The actual entity</returns>
        public T ToEntity(IClientSessionHandle session = null)
        {
            return new Find<T>(session).One(ID);
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database.
        /// </summary>
        /// <param name="session">An optional session</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <returns>A Task containing the actual entity</returns>
        public Task<T> ToEntityAsync(IClientSessionHandle session = null, CancellationToken cancellation = default)
        {
            return (new Find<T>(session)).OneAsync(ID, cancellation);
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database with a projection.
        /// </summary>
        /// <param name="projection">x => new Test { PropName = x.Prop }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <returns>The actual projected entity</returns>
        public T ToEntity(Expression<Func<T, T>> projection, IClientSessionHandle session = null)
        {
            return new Find<T>(session)
                        .Match(ID)
                        .Project(projection)
                        .Execute()
                        .SingleOrDefault();
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database with a projection.
        /// </summary>
        /// <param name="projection">x => new Test { PropName = x.Prop }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name = "cancellation" > An optional cancellation token</param>
        /// <returns>A Task containing the actual projected entity</returns>
        public async Task<T> ToEntityAsync(Expression<Func<T, T>> projection, IClientSessionHandle session = null, CancellationToken cancellation = default)
        {
            return (await new Find<T>(session)
                        .Match(ID)
                        .Project(projection)
                        .ExecuteAsync(cancellation).ConfigureAwait(false))
                   .SingleOrDefault();
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database with a projection.
        /// </summary>
        /// <param name="projection">p=> p.Include("Prop1").Exclude("Prop2")</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <returns>The actual projected entity</returns>
        public T ToEntity(Func<ProjectionDefinitionBuilder<T>, ProjectionDefinition<T, T>> projection, IClientSessionHandle session = null)
        {
            return new Find<T>(session)
                        .Match(ID)
                        .Project(projection)
                        .Execute()
                        .SingleOrDefault();
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database with a projection.
        /// </summary>
        /// <param name="projection">p=> p.Include("Prop1").Exclude("Prop2")</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name = "cancellation" > An optional cancellation token</param>
        /// <returns>A Task containing the actual projected entity</returns>
        public async Task<T> ToEntityAsync(Func<ProjectionDefinitionBuilder<T>, ProjectionDefinition<T, T>> projection, IClientSessionHandle session = null, CancellationToken cancellation = default)
        {
            return (await new Find<T>(session)
                        .Match(ID)
                        .Project(projection)
                        .ExecuteAsync(cancellation).ConfigureAwait(false))
                   .SingleOrDefault();
        }
    }
}
