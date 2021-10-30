using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace MongoDB.Entities
{
    public sealed partial class Many<TChild> where TChild : IEntity
    {
        /// <summary>
        /// An IAggregateFluent of JoinRecords for this relationship
        /// </summary>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IAggregateFluent<JoinRecord> JoinFluent(IClientSessionHandle session = null, AggregateOptions options = null)
        {
            return session == null
                ? JoinCollection.Aggregate(options)
                : JoinCollection.Aggregate(session, options);
        }

        /// <summary>
        /// Get an IAggregateFluent of parents matching a supplied IAggregateFluent of children for this relationship.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent IEntity</typeparam>
        /// <param name="children">An IAggregateFluent of children</param>
        public IAggregateFluent<TParent> ParentsFluent<TParent>(IAggregateFluent<TChild> children) where TParent : IEntity
        {
            if (typeof(TParent) == typeof(TChild)) throw new InvalidOperationException("Both parent and child types cannot be the same");

            if (isInverse)
            {
                return children
                       .Lookup<TChild, JoinRecord, Joined<JoinRecord>>(
                            JoinCollection,
                            c => c.ID,
                            r => r.ParentID,
                            j => j.Results)
                       .ReplaceRoot(j => j.Results[0])
                       .Lookup<JoinRecord, TParent, Joined<TParent>>(
                            DB.Collection<TParent>(null),
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
                            DB.Collection<TParent>(null),
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
        /// <param name="options">An optional AggregateOptions object</param>
        public IAggregateFluent<TParent> ParentsFluent<TParent>(string childID, IClientSessionHandle session = null, AggregateOptions options = null) where TParent : IEntity
        {
            return ParentsFluent<TParent>(new[] { childID }, session, options);
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

            if (isInverse)
            {
                return JoinFluent(session, options)
                       .Match(f => f.In(j => j.ParentID, childIDs))
                       .Lookup<JoinRecord, TParent, Joined<TParent>>(
                            DB.Collection<TParent>(null),
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
                            DB.Collection<TParent>(null),
                            r => r.ParentID,
                            p => p.ID,
                            j => j.Results)
                       .ReplaceRoot(j => j.Results[0])
                       .Distinct();
            }
        }

        /// <summary>
        /// An IAggregateFluent of child Entities for the parent.
        /// </summary>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IAggregateFluent<TChild> ChildrenFluent(IClientSessionHandle session = null, AggregateOptions options = null)
        {
            parent.ThrowIfUnsaved();

            if (isInverse)
            {
                return JoinFluent(session, options)
                        .Match(f => f.Eq(r => r.ChildID, parent.ID))
                        .Lookup<JoinRecord, TChild, Joined<TChild>>(
                            DB.Collection<TChild>(null),
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
                            DB.Collection<TChild>(null),
                            r => r.ChildID,
                            c => c.ID,
                            j => j.Results)
                        .ReplaceRoot(j => j.Results[0]);
            }
        }

        private class Joined<T> : JoinRecord
        {
            public T[] Results { get; set; }
        }
    }
}
