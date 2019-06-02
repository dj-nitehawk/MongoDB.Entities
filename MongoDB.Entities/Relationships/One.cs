using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// A one-to-one reference for an Entity.
    /// </summary>
    /// <typeparam name="T">Any type that inherits from Entity</typeparam>
    public class One<T> where T : Entity
    {
        /// <summary>
        /// The Id of the entity referenced by this instance.
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string ID { get; set; }

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
        /// Fetches the actual entity this reference represents from the database.
        /// </summary>
        /// <returns>The actual entity</returns>
        public T ToEntity()
        {
            return ToEntityAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database.
        /// </summary>
        /// <returns>A Task containing the actual entity</returns>
        async public Task<T> ToEntityAsync()
        {
            return await DB.Queryable<T>().SingleOrDefaultAsync(e => e.ID.Equals(ID));
        }
    }
}
