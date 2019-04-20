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
    public class Reference<T> where T : Entity
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
        public Reference(T entity)
        {
            CheckIfEntityIsSaved(entity.ID);
            Id = entity.ID;
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database.
        /// </summary>
        /// <returns>The actual entity</returns>
        public T ToEntity()
        {
            return DB.Collection<T>().SingleOrDefault(e => e.ID.Equals(Id));
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database.
        /// </summary>
        /// <returns>A Task containing the actual entity</returns>
        public Task<T> ToEntityAsync()
        {
            return DB.Collection<T>().SingleOrDefaultAsync(e => e.ID.Equals(Id));
        }

        private void CheckIfEntityIsSaved(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new InvalidOperationException("Please save the entity before adding references to it!");
        }
    }

    ////todo: remarks
    //public class ReferenceCollection<T> where T : Entity
    //{
    //    [BsonRepresentation(BsonType.ObjectId)]
    //    public Collection<string> IDs { get; set; } = new Collection<string>();

    //    //todo: remarks
    //    internal ReferenceCollection(T entity)
    //    {
    //        CheckIfEntityIsSaved(entity);
    //        IDs.Add(entity.ID);
    //    }

    //    //todo: remarks
    //    internal ReferenceCollection(IEnumerable<T> entities)
    //    {
    //        var hasUnsaved = (from e in entities
    //                          where string.IsNullOrEmpty(e.ID)
    //                          select e).Any();

    //        if (hasUnsaved) throw new InvalidCastException("Save all entities before attempting to set references to them!");

    //        foreach (var e in entities)
    //        {
    //            IDs.Add(e.ID);
    //        }
    //    }

    //    //todo: collection management...
    //    public void Add(T entity)
    //    {
    //        CheckIfEntityIsSaved(entity);
    //        IDs.Add(entity.ID);
    //    }

    //    //public void Remove(T entity)
    //    //{
    //    //    CheckIfEntityIsSaved(entity);
    //    //}

    //    //todo: get entities

    //    private void CheckIfEntityIsSaved(T entity)
    //    {
    //        if (string.IsNullOrEmpty(entity.ID)) throw new InvalidOperationException("Please save the entity before adding references to it!");
    //    }
    //}
}
