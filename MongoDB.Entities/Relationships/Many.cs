using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public abstract class ManyBase
    {
        //shared state for all Many<T> instances
        internal static HashSet<string> indexedCollections = new HashSet<string>();
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
        private bool inverse = false;
        private Entity parent = null;
        private IMongoCollection<Reference> collection = null;

        /// <summary>
        /// IQueryable of the join collection for this relationship
        /// </summary>
        /// <param name="options">An optional AggregateOptions object</param>
        public IMongoQueryable<Reference> JoinQueryable(AggregateOptions options = null) => collection.AsQueryable(options);

        /// <summary>
        /// The IAggregateFluent of the join collection for this relationship
        /// </summary>
        /// <param name="options">An optional AggregateOptions object</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public IAggregateFluent<Reference> JoinFluent(IClientSessionHandle session = null, AggregateOptions options = null)
        {
            return session == null
                ? collection.Aggregate(options)
                : collection.Aggregate(session, options);
        }

        /// <summary>
        /// An IQueryable of child Entities for the parent.
        /// </summary>
        public IMongoQueryable<TChild> ChildrenQueryable()
        {
            parent.ThrowIfUnsaved();

            if (inverse)
            {
                var myRefs = from r in JoinQueryable()
                             where r.ChildID.Equals(parent.ID)
                             select r;

                return from r in myRefs
                       join c in DB.Queryable<TChild>() on r.ParentID equals c.ID
                       select c;
            }
            else
            {
                var myRefs = from r in JoinQueryable()
                             where r.ParentID.Equals(parent.ID)
                             select r;

                return from r in myRefs
                       join c in DB.Queryable<TChild>() on r.ChildID equals c.ID
                       select c;
            }
        }

        /// <summary>
        /// An IAggregateFluent of child Entities for the parent.
        /// </summary>
        /// <param name="session"></param>
        public IAggregateFluent<TChild> ChildrenFluent(IClientSessionHandle session = null)
        {
            parent.ThrowIfUnsaved();

            if (inverse)
            {
                return JoinFluent(session)
                        .Match(f => f.Eq(r => r.ChildID, parent.ID))
                        .Lookup<Reference, TChild, Joined>(
                            foreignCollection: DB.Collection<TChild>(),
                            localField: (Reference r) => r.ParentID,
                            foreignField: (TChild c) => c.ID, j => j.Children)
                        .ReplaceRoot(j => j.Children[0]);
            }
            else
            {
                return JoinFluent(session)
                        .Match(f => f.Eq(r => r.ParentID, parent.ID))
                        .Lookup<Reference, TChild, Joined>(
                            foreignCollection: DB.Collection<TChild>(),
                            localField: (Reference r) => r.ChildID,
                            foreignField: (TChild c) => c.ID, j => j.Children)
                        .ReplaceRoot(j => j.Children[0]);
            }
        }

        internal Many() => throw new InvalidOperationException("Parameterless constructor is disabled!");

        internal Many(object parent, string property)
        {
            Init((dynamic)parent, property);
        }

        private void Init<TParent>(TParent parent, string property) where TParent : Entity
        {
            this.parent = parent;
            inverse = false;
            collection = DB.GetRefCollection($"[{DB.GetCollectionName<TParent>()}~{DB.GetCollectionName<TChild>()}({property})]");
            SetupIndexes(collection);
        }

        internal Many(object parent, string propertyParent, string propertyChild, bool isInverse)
        {
            Init((dynamic)parent, propertyParent, propertyChild, isInverse);
        }

        private void Init<TParent>(TParent parent, string propertyParent, string propertyChild, bool isInverse) where TParent : Entity
        {
            this.parent = parent;
            inverse = isInverse;

            if (inverse)
            {
                collection = DB.GetRefCollection($"[({propertyParent}){DB.GetCollectionName<TChild>()}~{DB.GetCollectionName<TParent>()}({propertyChild})]");
            }
            else
            {
                collection = DB.GetRefCollection($"[({propertyChild}){DB.GetCollectionName<TParent>()}~{DB.GetCollectionName<TChild>()}({propertyParent})]");
            }

            SetupIndexes(collection);
        }

        private static void SetupIndexes(IMongoCollection<Reference> collection)
        {
            //only create indexes once per unique ref collection
            if (!indexedCollections.Contains(collection.CollectionNamespace.CollectionName))
            {
                indexedCollections.Add(collection.CollectionNamespace.CollectionName);
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
        /// <param name="session">An optional session if using within a transaction</param>
        public void Add(TChild child, IClientSessionHandle session = null)
        {
            AddAsync(child, session).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Adds a new child reference.
        /// <para>WARNING: Make sure to save the parent and child Entities before calling this method.</para>
        /// </summary>
        /// <param name="child">The child Entity to add.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        async public Task AddAsync(TChild child, IClientSessionHandle session = null)
        {
            parent.ThrowIfUnsaved();
            child.ThrowIfUnsaved();

            Reference rfrnc = null;

            if (inverse)
            {
                rfrnc = await JoinQueryable().SingleOrDefaultAsync(r =>
                                                                   r.ChildID.Equals(parent.ID) &&
                                                                   r.ParentID.Equals(child.ID));
                if (rfrnc == null)
                {
                    rfrnc = new Reference()
                    {
                        ID = ObjectId.GenerateNewId().ToString(),
                        ModifiedOn = DateTime.UtcNow,
                        ParentID = child.ID,
                        ChildID = parent.ID,
                    };
                }
            }
            else
            {
                rfrnc = await JoinQueryable().SingleOrDefaultAsync(r =>
                                                                   r.ParentID.Equals(parent.ID) &&
                                                                   r.ChildID.Equals(child.ID));
                if (rfrnc == null)
                {
                    rfrnc = new Reference()
                    {
                        ID = ObjectId.GenerateNewId().ToString(),
                        ModifiedOn = DateTime.UtcNow,
                        ParentID = parent.ID,
                        ChildID = child.ID,
                    };
                }
            }

            await (session == null
                   ? collection.ReplaceOneAsync(x => x.ID.Equals(rfrnc.ID), rfrnc, new UpdateOptions() { IsUpsert = true })
                   : collection.ReplaceOneAsync(session, x => x.ID.Equals(rfrnc.ID), rfrnc, new UpdateOptions() { IsUpsert = true }));
        }

        /// <summary>
        /// Removes a child reference.
        /// </summary>
        /// <param name="child">The child Entity to remove the reference of.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public void Remove(TChild child, IClientSessionHandle session = null)
        {
            RemoveAsync(child, session).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Removes a child reference.
        /// </summary>
        /// <param name="child">The child Entity to remove the reference of.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        async public Task RemoveAsync(TChild child, IClientSessionHandle session = null)
        {
            if (inverse)
            {
                await (session == null
                       ? collection.DeleteOneAsync(r => r.ParentID.Equals(child.ID))
                       : collection.DeleteOneAsync(session, r => r.ParentID.Equals(child.ID)));
            }
            else
            {
                await (session == null
                       ? collection.DeleteOneAsync(r => r.ChildID.Equals(child.ID))
                       : collection.DeleteOneAsync(session, r => r.ChildID.Equals(child.ID)));

            }
        }

        private class Joined : Reference
        {
            public TChild[] Children { get; set; }
        }
    }
}
