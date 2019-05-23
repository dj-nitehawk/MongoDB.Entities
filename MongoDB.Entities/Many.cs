using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public class ManyBase
    {
        //shared state for all Many<T> instances
        internal static HashSet<string> _indexedCollections = new HashSet<string>();
    }

    /// <summary>
    /// A one-to-many/many-to-many reference collection.
    /// <para>WARNING: You have to initialize all instances of this class before accessing any of it's members.</para>
    /// <para>Initialize from the constructor of the parent entity as follows:</para>
    /// <code>this.InitOneToMany(() => Property)</code>
    /// <code>this.InitManyToMany(() => Property, x => x.OtherProperty)</code>
    /// </summary>
    /// <typeparam name="TChild">Type of the child Entity.</typeparam>
    public class Many<TChild> : ManyBase where TChild : Entity
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
            _parent = parent;
            _inverse = false;
            _collection = DB.GetRefCollection($"[{DB.GetCollectionName<TParent>()}~{DB.GetCollectionName<TChild>()}({property})]");
            SetupIndexes(_collection);
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
                _collection = DB.GetRefCollection($"[({propertyParent}){DB.GetCollectionName<TChild>()}~{DB.GetCollectionName<TParent>()}({propertyChild})]");
            }
            else
            {
                _collection = DB.GetRefCollection($"[({propertyChild}){DB.GetCollectionName<TParent>()}~{DB.GetCollectionName<TChild>()}({propertyParent})]");
            }

            SetupIndexes(_collection);
        }

        private static void SetupIndexes(IMongoCollection<Reference> collection)
        {
            //only create indexes once per unique ref collection
            if (!_indexedCollections.Contains(collection.CollectionNamespace.CollectionName))
            {
                _indexedCollections.Add(collection.CollectionNamespace.CollectionName);
                Task.Run(() =>
                {
                    collection.Indexes.CreateMany(
                    new[] {
                        new CreateIndexModel<Reference>(
                            Builders<Reference>.IndexKeys.Ascending(r => r.ParentID),
                            new CreateIndexOptions
                            {
                                Background = true,
                                Name = "[ParentID]"
                            })
                        ,
                        new CreateIndexModel<Reference>(
                            Builders<Reference>.IndexKeys.Ascending(r => r.ChildID),
                            new CreateIndexOptions
                            {
                                Background = true,
                                Name = "[ChildID]"
                            })
                    });
                });
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
