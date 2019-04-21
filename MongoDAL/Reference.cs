using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
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
            return DB.Collection<T>().SingleOrDefault(e => e.ID.Equals(ID));
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database.
        /// </summary>
        /// <returns>A Task containing the actual entity</returns>
        public Task<T> ToEntityAsync()
        {
            return DB.Collection<T>().SingleOrDefaultAsync(e => e.ID.Equals(ID));
        }
    }

    //todo: doc
    internal class Reference : Entity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string ParentID { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string ChildID { get; set; }
    }

    //todo: doc
    public class Many<TParent, TChild> where TParent : Entity where TChild : Entity
    {
        private TParent _parent = null;
        private IMongoCollection<Reference> _collection = null;

        public string StoredIn { get; set; }

        public IMongoQueryable<TChild> Collection
        {
            get
            {
                var myRefs = from r in _collection.AsQueryable()
                             where r.ParentID.Equals(_parent.ID)
                             select r;

                return from r in myRefs
                       join c in DB.Collection<TChild>() on r.ChildID equals c.ID into children
                       from ch in children
                       select ch;
            }
        }

        internal Many(TParent parent)
        {
            _parent = parent;
            _collection = DB.Coll<TParent, TChild>();
            StoredIn = typeof(TParent).Name + "_" + typeof(TChild).Name;
        }

        private Reference RefToSave(TChild child)
        {
            _parent.ThrowIfUnsaved();
            child.ThrowIfUnsaved();

            var refr = _collection.AsQueryable()
                .SingleOrDefault(r =>
                r.ParentID.Equals(_parent.ID) &&
                r.ChildID.Equals(child.ID));

            if (refr == null)
            {
                refr = new Reference()
                {
                    ID = ObjectId.GenerateNewId().ToString(),
                    ModifiedOn = DateTime.UtcNow,
                    ParentID = _parent.ID,
                    ChildID = child.ID,
                };
            }

            return refr;
        }

        public void Add(TChild child)
        {
            var refr = RefToSave(child);

            _collection.ReplaceOne(
                x => x.ID.Equals(refr.ID),
                refr,
                new UpdateOptions() { IsUpsert = true });
        }

        public Task AddAsync(TChild child)
        {
            var refr = RefToSave(child);

            return _collection.ReplaceOneAsync(
                 x => x.ID.Equals(refr.ID),
                 refr,
                 new UpdateOptions() { IsUpsert = true });
        }

        //todo: Remove and RemoveAsync

    }
}
