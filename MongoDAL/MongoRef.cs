using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDAL
{
    /// <summary>
    /// Represents a reference to an entity in MongoDB.
    /// </summary>
    /// <typeparam name="T">Any type that inherits from MongoEntity</typeparam>
    public class MongoRef<T> where T : MongoEntity
    {
        /// <summary>
        /// The Id of the entity referenced by this instance.
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        /// <summary>
        /// Initializes a reference to an entity in MongoDB. 
        /// </summary>
        /// <param name="entity">The actual entity this reference represents.</param>
        public MongoRef(T entity)
        {
            CheckIfEntityIsSaved(entity.Id);
            Id = entity.Id;
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database.
        /// </summary>
        /// <returns>The actual entity</returns>
        public T FetchEntity()
        {
            return DB.Collection<T>().SingleOrDefault(e => e.Id.Equals(Id));
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database.
        /// </summary>
        /// <returns>A Task containing the actual entity</returns>
        public Task<T> FetchEntityAsync()
        {
            return DB.Collection<T>().SingleOrDefaultAsync(e => e.Id.Equals(Id));
        }

        private void CheckIfEntityIsSaved(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new InvalidOperationException("Please save the entity before adding references to it!");
        }
    }

    //todo: remarks
    public class MongoRefs<T> where T : MongoEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public Collection<string> IDs { get; set; } = new Collection<string>();

        //todo: remarks
        internal MongoRefs(T entity)
        {
            CheckIfEntityIsSaved(entity);
            IDs.Add(entity.Id);
        }

        //todo: remarks
        internal MongoRefs(IEnumerable<T> entities)
        {
            var hasUnsaved = (from e in entities
                              where string.IsNullOrEmpty(e.Id)
                              select e).Any();

            if (hasUnsaved) throw new InvalidCastException("Save all entities before attempting to set references to them!");

            foreach (var e in entities)
            {
                IDs.Add(e.Id);
            }
        }

        //todo: collection management...
        public void Add(T entity)
        {
            CheckIfEntityIsSaved(entity);
            IDs.Add(entity.Id);
        }

        //public void Remove(T entity)
        //{
        //    CheckIfEntityIsSaved(entity);
        //}

        //todo: get entities

        private void CheckIfEntityIsSaved(T entity)
        {
            if (string.IsNullOrEmpty(entity.Id)) throw new InvalidOperationException("Please save the entity before adding references to it!");
        }
    }
}
