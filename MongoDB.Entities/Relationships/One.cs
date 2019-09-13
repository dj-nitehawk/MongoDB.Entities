using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents a one-to-one relationship with an Entity.
    /// </summary>
    /// <typeparam name="T">Any type that inherits from Entity</typeparam>
    public class One<T> where T : Entity
    {
        private string db = null;

        /// <summary>
        /// The Id of the entity referenced by this instance.
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string ID { get; set; }

        public One()
        {
            var attribute = typeof(T).GetTypeInfo().GetCustomAttribute<DatabaseAttribute>();
            if (attribute != null)
            {
                db = attribute.Name;
            }
        }

        /// <summary>
        /// Initializes a reference to an entity in MongoDB. 
        /// </summary>
        /// <param name="entity">The actual entity this reference represents.</param>
        internal One(T entity)
        {
            entity.ThrowIfUnsaved();
            ID = entity.ID;
            db = entity.Database();
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database.
        /// </summary>
        /// <returns>The actual entity</returns>
        public T ToEntity(IClientSessionHandle session = null)
        {
            return ToEntityAsync(session).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database.
        /// </summary>
        /// <returns>A Task containing the actual entity</returns>
        public async Task<T> ToEntityAsync(IClientSessionHandle session = null)
        {
            return await (new Find<T>(session, db)).OneAsync(ID);
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database with a projection.
        /// </summary>
        /// <param name="projection">x => new Test { PropName = x.Prop }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <returns>The actual projected entity</returns>
        public T ToEntity(Expression<Func<T, T>> projection, IClientSessionHandle session = null)
        {
            return ToEntityAsync(projection, session).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database with a projection.
        /// </summary>
        /// <param name="projection">x => new Test { PropName = x.Prop }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <returns>A Task containing the actual projected entity</returns>
        public async Task<T> ToEntityAsync(Expression<Func<T, T>> projection, IClientSessionHandle session = null)
        {
            return (await
                        (new Find<T>(session, db))
                                .Match(ID)
                                .Project(projection)
                                .ExecuteAsync()
                                ).FirstOrDefault();
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database with a projection.
        /// </summary>
        /// <param name="projection">p=> p.Include("Prop1").Exclude("Prop2")</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <returns>The actual projected entity</returns>
        public T ToEntity(Func<ProjectionDefinitionBuilder<T>, ProjectionDefinition<T, T>> projection, IClientSessionHandle session = null)
        {
            return ToEntityAsync(projection, session).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database with a projection.
        /// </summary>
        /// <param name="projection">p=> p.Include("Prop1").Exclude("Prop2")</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <returns>A Task containing the actual projected entity</returns>
        public async Task<T> ToEntityAsync(Func<ProjectionDefinitionBuilder<T>, ProjectionDefinition<T, T>> projection, IClientSessionHandle session = null)
        {
            return (await
                        (new Find<T>(session, db))
                                .Match(ID)
                                .Project(projection)
                                .ExecuteAsync()
                                ).FirstOrDefault();
        }
    }
}
