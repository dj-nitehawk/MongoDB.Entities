using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// A one-to-many reference collection.
    /// <para>You have to initialize all instances of this class before accessing any of it's members.</para>
    /// <para>Use as follows in the constructor of enclosing class:</para>
    /// <code>Property = Property.Initialize(this);</code>
    /// </summary>
    /// <typeparam name="TChild">Type of the child Entity.</typeparam>
    public class Many<TChild> where TChild : Entity
    {
        private Entity _parent = null;
        private IMongoCollection<Reference> _collection = null;

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

        internal Many() => throw new InvalidOperationException("Parameterless constructor is disabled!");

        internal Many(object parent)
        {
            Init((dynamic)parent);
        }

        private void Init<TParent>(TParent parent) where TParent : Entity
        {
            _parent = parent;
            _collection = DB.Coll<TParent, TChild>();
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
