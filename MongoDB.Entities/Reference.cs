using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    internal class Reference : Entity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string ParentID { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string ChildID { get; set; }
    }

    /// <summary>
    /// A one-to-one reference for an Entity.
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

    /// <summary>
    /// A one-to-many reference collection.
    /// <para>You have to initialize all instances of this class before accessing any of it's members.</para>
    /// <para>Use as follows in the constructor of enclosing class:</para>
    /// <code>Property = Property.Initialize(this);</code>
    /// </summary>
    /// <typeparam name="TParent">Type of the parent Entity.</typeparam>
    /// <typeparam name="TChild">Type of the child Entity.</typeparam>
    public class Many<TParent, TChild> where TParent : Entity where TChild : Entity
    {
        private TParent _parent = null;
        private IMongoCollection<Reference> _collection = null;

        /// <summary>
        /// The name of the collection where the references are stored in MongoDB.
        /// </summary>
        public string StoredIn { get; set; }

        /// <summary>
        /// An IQueryable collection of child Entities.
        /// </summary>
        public IMongoQueryable<TChild> Collection()
        {
            var myRefs = from r in _collection.AsQueryable()
                         where r.ParentID.Equals(_parent.ID)
                         select r;

            return from r in myRefs
                   join c in DB.Collection<TChild>() on r.ChildID equals c.ID into children
                   from ch in children
                   select ch;
        }

        internal Many(TParent parent)
        {
            _parent = parent;
            _collection = DB.Coll<TParent, TChild>();
            StoredIn = typeof(TParent).Name + "_" + typeof(TChild).Name;
        }

        /// <summary>
        /// Adds a new child reference.
        /// <para>WARNING: Make sure to save the enclosing/parent Entity before calling this method.</para>
        /// </summary>
        /// <param name="child">The child Entity to add.</param>
        public void Add(TChild child)
        {
            AddAsync(child).Wait();
        }

        /// <summary>
        /// Adds a new child reference.
        /// <para>WARNING: Make sure to save the parent and child Entities before calling this method.</para>
        /// </summary>
        /// <param name="child">The child Entity to add.</param>
        public Task AddAsync(TChild child)
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

            return _collection.ReplaceOneAsync(x => x.ID.Equals(refr.ID),
                                                    refr,
                                                    new UpdateOptions() { IsUpsert = true });
        }

        /// <summary>
        /// Removes a child reference.
        /// </summary>
        /// <param name="child">The child Entity to remove the reference of.</param>
        public void Remove(TChild child)
        {
            RemoveAsync(child).Wait();
        }

        /// <summary>
        /// Removes a child reference.
        /// </summary>
        /// <param name="child">The child Entity to remove the reference of.</param>
        public Task RemoveAsync(TChild child)
        {
            return _collection.DeleteOneAsync(r => r.ChildID.Equals(child.ID));
        }
    }
}
