using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// A one-to-many/many-to-many reference collection.
    /// <para>WARNING: You have to initialize all instances of this class before accessing any of it's members.</para>
    /// <para>Initialize from the constructor of the parent entity as follows:</para>
    /// <code>this.InitOneToMany(() => Property)</code>
    /// <code>this.InitManyToMany(() => Property, x => x.OtherProperty)</code>
    /// </summary>
    /// <typeparam name="TChild">Type of the child Entity.</typeparam>
    public class Many<TChild> where TChild : Entity
    {
        private bool _inverse = false;
        private Entity _parent = null;
        private IMongoCollection<Reference> _collection = null;

        /// <summary>
        /// An IQueryable collection of child Entities for the parent.
        /// </summary>
        public IMongoQueryable<TChild> Collection()
        {
            _parent.ThrowIfUnsaved();

            if (_inverse)
            {
                var myRefs = from r in _collection.AsQueryable()
                             where r.ChildID.Equals(_parent.ID)
                             select r;

                return from r in myRefs
                       join c in DB.Collection<TChild>() on r.ParentID equals c.ID into children
                       from ch in children
                       select ch;
            }
            else
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

        internal Many() => throw new InvalidOperationException("Parameterless constructor is disabled!");

        internal Many(object parent, string property)
        {
            Init((dynamic)parent, property);
        }

        private void Init<TParent>(TParent parent, string property) where TParent : Entity
        {
            _inverse = false;
            _parent = parent;
            _collection = DB.GetRefCollection($"[{ typeof(TParent).Name}~{ typeof(TChild).Name}({property})]");
        }

        internal Many(object parent, string propertyParent, string propertyChild, bool isInverse)
        {
            Init((dynamic)parent, propertyParent, propertyChild, isInverse);
        }

        private void Init<TParent>(TParent parent, string propertyParent, string propertyChild, bool isInverse) where TParent : Entity
        {
            _parent = parent;
            _inverse = isInverse;

            if (_inverse)
            {
                _collection = DB.GetRefCollection($"[({propertyParent}){typeof(TChild).Name}~{typeof(TParent).Name}({propertyChild})]");
            }
            else
            {
                _collection = DB.GetRefCollection($"[({propertyChild}){typeof(TParent).Name}~{typeof(TChild).Name}({propertyParent})]");
            }
        }

        /// <summary>
        /// Adds a new child reference.
        /// <para>WARNING: Make sure to save the enclosing/parent Entity before calling this method.</para>
        /// </summary>
        /// <param name="child">The child Entity to add.</param>
        public void Add(TChild child)
        {
            AddAsync(child).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Adds a new child reference.
        /// <para>WARNING: Make sure to save the parent and child Entities before calling this method.</para>
        /// </summary>
        /// <param name="child">The child Entity to add.</param>
        async public Task AddAsync(TChild child)
        {
            _parent.ThrowIfUnsaved();
            child.ThrowIfUnsaved();

            Reference rfrnc = null;

            if (_inverse)
            {
                rfrnc = await _collection.AsQueryable()
                                         .SingleOrDefaultAsync(r =>
                                                               r.ChildID.Equals(_parent.ID) &&
                                                               r.ParentID.Equals(child.ID));
                if (rfrnc == null)
                {
                    rfrnc = new Reference()
                    {
                        ID = ObjectId.GenerateNewId().ToString(),
                        ModifiedOn = DateTime.UtcNow,
                        ParentID = child.ID,
                        ChildID = _parent.ID,
                    };
                }
            }
            else
            {
                rfrnc = await _collection.AsQueryable()
                                          .SingleOrDefaultAsync(r =>
                                                                r.ParentID.Equals(_parent.ID) &&
                                                                r.ChildID.Equals(child.ID));
                if (rfrnc == null)
                {
                    rfrnc = new Reference()
                    {
                        ID = ObjectId.GenerateNewId().ToString(),
                        ModifiedOn = DateTime.UtcNow,
                        ParentID = _parent.ID,
                        ChildID = child.ID,
                    };
                }
            }

            await _collection.ReplaceOneAsync(x => x.ID.Equals(rfrnc.ID),
                                              rfrnc,
                                              new UpdateOptions() { IsUpsert = true });
        }

        /// <summary>
        /// Removes a child reference.
        /// </summary>
        /// <param name="child">The child Entity to remove the reference of.</param>
        public void Remove(TChild child)
        {
            RemoveAsync(child).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Removes a child reference.
        /// </summary>
        /// <param name="child">The child Entity to remove the reference of.</param>
        async public Task RemoveAsync(TChild child)
        {
            if (_inverse)
            {
                await _collection.DeleteOneAsync(r => r.ParentID.Equals(child.ID));
            }
            else
            {
                await _collection.DeleteOneAsync(r => r.ChildID.Equals(child.ID));
            }
        }
    }
}
