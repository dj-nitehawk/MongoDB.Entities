using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Core;
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

        internal static object CreateChild(Type type)
        {
            return Activator.CreateInstance(type);
        }
    }

    /// <summary>
    /// Represents a one-to-many/many-to-many relationship between two Entities.
    /// <para>WARNING: You have to initialize all instances of this class before accessing any of it's members.</para>
    /// <para>Initialize from the constructor of the parent entity as follows:</para>
    /// <code>this.InitOneToMany(() => Property)</code>
    /// <code>this.InitManyToMany(() => Property, x => x.OtherProperty)</code>
    /// </summary>
    /// <typeparam name="TChild">Type of the child IEntity.</typeparam>
    public class Many<TChild> : ManyBase where TChild : IEntity
    {
        private string db = null;
        private bool inverse = false;
        private IEntity parent = null;


        /// <summary>
        /// Gets the IMongoCollection of JoinRecords for this relationship.
        /// <para>TIP: Try never to use this unless really neccessary.</para>
        /// </summary>
        public IMongoCollection<JoinRecord> JoinCollection { get; private set; } = null;

        /// <summary>
        /// An IQueryable of JoinRecords for this relationship
        /// </summary>
        /// <param name="options">An optional AggregateOptions object</param>
        public IMongoQueryable<JoinRecord> JoinQueryable(AggregateOptions options = null) => JoinCollection.AsQueryable(options);

        /// <summary>
        /// An IAggregateFluent of JoinRecords for this relationship
        /// </summary>
        /// <param name="options">An optional AggregateOptions object</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public IAggregateFluent<JoinRecord> JoinFluent(IClientSessionHandle session = null, AggregateOptions options = null)
        {
            return session == null
                ? JoinCollection.Aggregate(options)
                : JoinCollection.Aggregate(session, options);
        }

        /// <summary>
        /// Get an IQueryable of parents matching a single child ID for this relationship.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent IEntity</typeparam>
        /// <param name="childID">A child ID</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IMongoQueryable<TParent> ParentsQueryable<TParent>(string childID, AggregateOptions options = null) where TParent : IEntity
        {
            return ParentsQueryable<TParent>(new[] { childID }, options);
        }

        /// <summary>
        /// Get an IQueryable of parents matching multiple child IDs for this relationship.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent IEntity</typeparam>
        /// <param name="childIDs">An IEnumerable of child IDs</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IMongoQueryable<TParent> ParentsQueryable<TParent>(IEnumerable<string> childIDs, AggregateOptions options = null) where TParent : IEntity
        {
            if (typeof(TParent) == typeof(TChild)) throw new InvalidOperationException("Both parent and child types cannot be the same");

            if (inverse)
            {
                return JoinQueryable(options)
                       .Where(j => childIDs.Contains(j.ParentID))
                       .Join(
                           DB.Collection<TParent>(db),
                           j => j.ChildID,
                           p => p.ID,
                           (_, p) => p)
                       .Distinct();
            }
            else
            {
                return JoinQueryable(options)
                       .Where(j => childIDs.Contains(j.ChildID))
                       .Join(
                           DB.Collection<TParent>(db),
                           j => j.ParentID,
                           p => p.ID,
                           (_, p) => p)
                       .Distinct();
            }
        }

        /// <summary>
        /// Get an IQueryable of parents matching a supplied IQueryable of children for this relationship.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent IEntity</typeparam>
        /// <param name="children">An IQueryable of children</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IMongoQueryable<TParent> ParentsQueryable<TParent>(IMongoQueryable<TChild> children, AggregateOptions options = null) where TParent : IEntity
        {
            if (typeof(TParent) == typeof(TChild)) throw new InvalidOperationException("Both parent and child types cannot be the same");

            if (inverse)
            {
                return children
                        .Join(
                             JoinQueryable(options),
                             c => c.ID,
                             j => j.ParentID,
                             (_, j) => j)
                        .Join(
                           DB.Collection<TParent>(db),
                           j => j.ChildID,
                           p => p.ID,
                           (_, p) => p)
                        .Distinct();
            }
            else
            {
                return children
                       .Join(
                            JoinQueryable(options),
                            c => c.ID,
                            j => j.ChildID,
                            (_, j) => j)
                       .Join(
                            DB.Collection<TParent>(db),
                            j => j.ParentID,
                            p => p.ID,
                            (_, p) => p)
                       .Distinct();
            }
        }

        /// <summary>
        /// Get an IAggregateFluent of parents matching a supplied IAggregateFluent of children for this relationship.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent IEntity</typeparam>
        /// <param name="children">An IAggregateFluent of children</param>
        public IAggregateFluent<TParent> ParentsFluent<TParent>(IAggregateFluent<TChild> children) where TParent : IEntity
        {
            if (typeof(TParent) == typeof(TChild)) throw new InvalidOperationException("Both parent and child types cannot be the same");

            if (inverse)
            {
                return children
                       .Lookup<TChild, JoinRecord, Joined<JoinRecord>>(
                            JoinCollection,
                            c => c.ID,
                            r => r.ParentID,
                            j => j.Results)
                       .ReplaceRoot(j => j.Results[0])
                       .Lookup<JoinRecord, TParent, Joined<TParent>>(
                            DB.Collection<TParent>(db),
                            r => r.ChildID,
                            p => p.ID,
                            j => j.Results)
                       .ReplaceRoot(j => j.Results[0])
                       .Distinct();
            }
            else
            {
                return children
                       .Lookup<TChild, JoinRecord, Joined<JoinRecord>>(
                            JoinCollection,
                            c => c.ID,
                            r => r.ChildID,
                            j => j.Results)
                       .ReplaceRoot(j => j.Results[0])
                       .Lookup<JoinRecord, TParent, Joined<TParent>>(
                            DB.Collection<TParent>(db),
                            r => r.ParentID,
                            p => p.ID,
                            j => j.Results)
                       .ReplaceRoot(j => j.Results[0])
                       .Distinct();
            }
        }

        /// <summary>
        /// Get an IAggregateFluent of parents matching a single child ID for this relationship.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent IEntity</typeparam>
        /// <param name="childID">An child ID</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public IAggregateFluent<TParent> ParentsFluent<TParent>(string childID, IClientSessionHandle session = null) where TParent : IEntity
        {
            return ParentsFluent<TParent>(new[] { childID }, session);
        }

        /// <summary>
        /// Get an IAggregateFluent of parents matching multiple child IDs for this relationship.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent IEntity</typeparam>
        /// <param name="childIDs">An IEnumerable of child IDs</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IAggregateFluent<TParent> ParentsFluent<TParent>(IEnumerable<string> childIDs, IClientSessionHandle session = null, AggregateOptions options = null) where TParent : IEntity
        {
            if (typeof(TParent) == typeof(TChild)) throw new InvalidOperationException("Both parent and child types cannot be the same");

            if (inverse)
            {
                return JoinFluent(session, options)
                       .Match(f => f.In(j => j.ParentID, childIDs))
                       .Lookup<JoinRecord, TParent, Joined<TParent>>(
                            DB.Collection<TParent>(db),
                            j => j.ChildID,
                            p => p.ID,
                            j => j.Results)
                       .ReplaceRoot(j => j.Results[0])
                       .Distinct();
            }
            else
            {
                return JoinFluent(session, options)
                       .Match(f => f.In(j => j.ChildID, childIDs))
                       .Lookup<JoinRecord, TParent, Joined<TParent>>(
                            DB.Collection<TParent>(db),
                            r => r.ParentID,
                            p => p.ID,
                            j => j.Results)
                       .ReplaceRoot(j => j.Results[0])
                       .Distinct();
            }
        }

        /// <summary>
        /// Get the number of children for a relationship
        /// </summary>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public long ChildrenCount(IClientSessionHandle session = null, CountOptions options = null)
        {
            return Run.Sync(() => ChildrenCountAsync(session, options));
        }

        /// <summary>
        /// Get the number of children for a relationship
        /// </summary>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public Task<long> ChildrenCountAsync(IClientSessionHandle session = null, CountOptions options = null)
        {
            parent.ThrowIfUnsaved();

            if (inverse)
            {
                return session == null
                       ? JoinCollection.CountDocumentsAsync(j => j.ChildID == parent.ID, options)
                       : JoinCollection.CountDocumentsAsync(session, j => j.ChildID == parent.ID, options);
            }
            else
            {
                return session == null
                       ? JoinCollection.CountDocumentsAsync(j => j.ParentID == parent.ID, options)
                       : JoinCollection.CountDocumentsAsync(session, j => j.ParentID == parent.ID, options);
            }
        }

        /// <summary>
        /// An IQueryable of child Entities for the parent.
        /// </summary>
        /// <param name="options">An optional AggregateOptions object</param>
        public IMongoQueryable<TChild> ChildrenQueryable(AggregateOptions options = null)
        {
            parent.ThrowIfUnsaved();

            if (inverse)
            {
                return JoinQueryable(options)
                       .Where(j => j.ChildID == parent.ID)
                       .Join(
                           DB.Collection<TChild>(db),
                           j => j.ParentID,
                           c => c.ID,
                           (_, c) => c);
            }
            else
            {
                return JoinQueryable(options)
                       .Where(j => j.ParentID == parent.ID)
                       .Join(
                           DB.Collection<TChild>(db),
                           j => j.ChildID,
                           c => c.ID,
                           (_, c) => c);
            }
        }

        /// <summary>
        /// An IAggregateFluent of child Entities for the parent.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IAggregateFluent<TChild> ChildrenFluent(IClientSessionHandle session = null, AggregateOptions options = null)
        {
            parent.ThrowIfUnsaved();

            if (inverse)
            {
                return JoinFluent(session, options)
                        .Match(f => f.Eq(r => r.ChildID, parent.ID))
                        .Lookup<JoinRecord, TChild, Joined<TChild>>(
                            DB.Collection<TChild>(db),
                            r => r.ParentID,
                            c => c.ID,
                            j => j.Results)
                        .ReplaceRoot(j => j.Results[0]);
            }
            else
            {
                return JoinFluent(session, options)
                        .Match(f => f.Eq(r => r.ParentID, parent.ID))
                        .Lookup<JoinRecord, TChild, Joined<TChild>>(
                            DB.Collection<TChild>(db),
                            r => r.ChildID,
                            c => c.ID,
                            j => j.Results)
                        .ReplaceRoot(j => j.Results[0]);
            }
        }

        internal Many() => throw new InvalidOperationException("Parameterless constructor is disabled!");

        internal Many(object parent, string property)
        {
            Init((dynamic)parent, property);
        }

        private void Init<TParent>(TParent parent, string property) where TParent : IEntity
        {
            this.parent = parent;
            db = parent.Database();
            inverse = false;
            JoinCollection = DB.GetRefCollection($"[{DB.GetCollectionName<TParent>()}~{DB.GetCollectionName<TChild>()}({property})]", db);
            SetupIndexes(JoinCollection);
        }

        internal Many(object parent, string propertyParent, string propertyChild, bool isInverse)
        {
            Init((dynamic)parent, propertyParent, propertyChild, isInverse);
        }

        private void Init<TParent>(TParent parent, string propertyParent, string propertyChild, bool isInverse) where TParent : IEntity
        {
            this.parent = parent;
            db = parent.Database();
            inverse = isInverse;

            if (inverse)
            {
                JoinCollection = DB.GetRefCollection($"[({propertyParent}){DB.GetCollectionName<TChild>()}~{DB.GetCollectionName<TParent>()}({propertyChild})]", db);
            }
            else
            {
                JoinCollection = DB.GetRefCollection($"[({propertyChild}){DB.GetCollectionName<TParent>()}~{DB.GetCollectionName<TChild>()}({propertyParent})]", db);
            }

            SetupIndexes(JoinCollection);
        }

        private static void SetupIndexes(IMongoCollection<JoinRecord> collection)
        {
            //only create indexes once per unique ref collection
            if (!indexedCollections.Contains(collection.CollectionNamespace.CollectionName))
            {
                indexedCollections.Add(collection.CollectionNamespace.CollectionName);
                collection.Indexes.CreateManyAsync(
                    new[] {
                        new CreateIndexModel<JoinRecord>(
                            Builders<JoinRecord>.IndexKeys.Ascending(r => r.ParentID),
                            new CreateIndexOptions
                            {
                                Background = true,
                                Name = "[ParentID]"
                            })
                        ,
                        new CreateIndexModel<JoinRecord>(
                            Builders<JoinRecord>.IndexKeys.Ascending(r => r.ChildID),
                            new CreateIndexOptions
                            {
                                Background = true,
                                Name = "[ChildID]"
                            })
                    });
            }
        }

        /// <summary>
        /// Adds a new child reference.
        /// <para>WARNING: Make sure to save the parent and child Entities before calling this method.</para>
        /// </summary>
        /// <param name="child">The child IEntity to add.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public void Add(TChild child, IClientSessionHandle session = null)
        {
            Run.Sync(() => AddAsync(child, session));
        }

        /// <summary>
        /// Adds a new child reference by ID.
        /// </summary>
        /// <param name="childID">The ID of the child entity to add.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public void Add(string childID, IClientSessionHandle session = null)
        {
            var child = (TChild)CreateChild(typeof(TChild));
            child.ID = childID;
            Run.Sync(() => AddAsync(child, session));
        }

        /// <summary>
        /// Adds a new child reference.
        /// <para>WARNING: Make sure to save the parent and child Entities before calling this method.</para>
        /// </summary>
        /// <param name="child">The child IEntity to add.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public async Task AddAsync(TChild child, IClientSessionHandle session = null)
        {
            parent.ThrowIfUnsaved();
            child.ThrowIfUnsaved();

            JoinRecord join = null;

            if (inverse)
            {
                join = await (session == null ?
                              JoinCollection.Find(r => r.ChildID == parent.ID && r.ParentID == child.ID).SingleOrDefaultAsync() :
                              JoinCollection.Find(session, r => r.ChildID == parent.ID && r.ParentID == child.ID).SingleOrDefaultAsync());

                if (join == null)
                {
                    join = new JoinRecord()
                    {
                        ID = ObjectId.GenerateNewId().ToString(),
                        ModifiedOn = DateTime.UtcNow,
                        ParentID = child.ID,
                        ChildID = parent.ID,
                    };
                }
                else
                {
                    return;
                }
            }
            else
            {
                join = await (session == null ?
                              JoinCollection.Find(r => r.ParentID == parent.ID && r.ChildID == child.ID).SingleOrDefaultAsync() :
                              JoinCollection.Find(session, r => r.ParentID == parent.ID && r.ChildID == child.ID).SingleOrDefaultAsync());

                if (join == null)
                {
                    join = new JoinRecord()
                    {
                        ID = ObjectId.GenerateNewId().ToString(),
                        ModifiedOn = DateTime.UtcNow,
                        ParentID = parent.ID,
                        ChildID = child.ID,
                    };
                }
                else
                {
                    return;
                }
            }

            await (session == null
                   ? JoinCollection.InsertOneAsync(join)
                   : JoinCollection.InsertOneAsync(session, join));
        }

        /// <summary>
        /// Removes a child reference.
        /// </summary>
        /// <param name="child">The child IEntity to remove the reference of.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public void Remove(TChild child, IClientSessionHandle session = null)
        {
            Run.Sync(() => RemoveAsync(child, session));
        }

        /// <summary>
        /// Removes a child reference by ID.
        /// </summary>
        /// <param name="childID">The ID of the child entity to remove the reference of.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public void Remove(string childID, IClientSessionHandle session = null)
        {
            var child = (TChild)CreateChild(typeof(TChild));
            child.ID = childID;
            Run.Sync(() => RemoveAsync(child, session));
        }

        /// <summary>
        /// Removes a child reference.
        /// </summary>
        /// <param name="child">The child IEntity to remove the reference of.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public Task RemoveAsync(TChild child, IClientSessionHandle session = null)
        {
            if (inverse)
            {
                return session == null
                       ? JoinCollection.DeleteOneAsync(r => r.ParentID.Equals(child.ID))
                       : JoinCollection.DeleteOneAsync(session, r => r.ParentID.Equals(child.ID));
            }
            else
            {
                return session == null
                       ? JoinCollection.DeleteOneAsync(r => r.ChildID.Equals(child.ID))
                       : JoinCollection.DeleteOneAsync(session, r => r.ChildID.Equals(child.ID));

            }
        }

        /// <summary>
        /// Overloaded operator for adding a child entity
        /// </summary>
        /// <param name="many">The left side of the + operand</param>
        /// <param name="child">The right side of the + operand</param>
        public static Many<TChild> operator +(Many<TChild> many, TChild child)
        {
            many.Add(child);
            return many;
        }

        /// <summary>
        /// Overloaded operator for adding a child entity by specifying only the childID
        /// </summary>
        /// <param name="many">The left side of the + operand</param>
        /// <param name="childID">The right side of the + operand</param>
        public static Many<TChild> operator +(Many<TChild> many, string childID)
        {
            var child = (TChild)CreateChild(typeof(TChild));
            child.ID = childID;
            many.Add(child);
            return many;
        }

        /// <summary>
        /// Overloaded operator for removing a child entity
        /// </summary>
        /// <param name="many">The left side of the - operand</param>
        /// <param name="child">The right side of the - operand</param>
        /// <returns></returns>
        public static Many<TChild> operator -(Many<TChild> many, TChild child)
        {
            many.Remove(child);
            return many;
        }

        /// <summary>
        /// Overloaded operator for removing a child entity by specifying only the childID
        /// </summary>
        /// <param name="many">The left side of the - operand</param>
        /// <param name="childID">The right side of the - operand</param>
        /// <returns></returns>
        public static Many<TChild> operator -(Many<TChild> many, string childID)
        {
            var child = (TChild)CreateChild(typeof(TChild));
            child.ID = childID;
            many.Remove(child);
            return many;
        }

        /// <summary>
        /// A class used to hold join results when joining relationships
        /// </summary>
        /// <typeparam name="T">The type of the resulting objects</typeparam>
        public class Joined<T> : JoinRecord
        {
            public T[] Results { get; set; }
        }
    }
}
