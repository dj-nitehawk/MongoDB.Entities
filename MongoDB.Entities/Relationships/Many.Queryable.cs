using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Entities
{
    public partial class Many<TChild> : IEnumerable<TChild> where TChild : IEntity
    {
        /// <summary>
        /// An IQueryable of JoinRecords for this relationship
        /// </summary>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IMongoQueryable<JoinRecord> JoinQueryable(IClientSessionHandle session = null, AggregateOptions options = null)
        {
            return session == null
                   ? JoinCollection.AsQueryable(options)
                   : JoinCollection.AsQueryable(session, options);
        }

        /// <summary>
        /// Get an IQueryable of parents matching a single child ID for this relationship.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent IEntity</typeparam>
        /// <param name="childID">A child ID</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IMongoQueryable<TParent> ParentsQueryable<TParent>(string childID, IClientSessionHandle session = null, AggregateOptions options = null) where TParent : IEntity
        {
            return ParentsQueryable<TParent>(new[] { childID }, session, options);
        }

        /// <summary>
        /// Get an IQueryable of parents matching multiple child IDs for this relationship.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent IEntity</typeparam>
        /// <param name="childIDs">An IEnumerable of child IDs</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IMongoQueryable<TParent> ParentsQueryable<TParent>(IEnumerable<string> childIDs, IClientSessionHandle session = null, AggregateOptions options = null) where TParent : IEntity
        {
            if (typeof(TParent) == typeof(TChild)) throw new InvalidOperationException("Both parent and child types cannot be the same");

            if (isInverse)
            {
                return JoinQueryable(session, options)
                       .Where(j => childIDs.Contains(j.ParentID))
                       .Join(
                           DB.Collection<TParent>(),
                           j => j.ChildID,
                           p => p.ID,
                           (_, p) => p)
                       .Distinct();
            }
            else
            {
                return JoinQueryable(session, options)
                       .Where(j => childIDs.Contains(j.ChildID))
                       .Join(
                           DB.Collection<TParent>(),
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
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IMongoQueryable<TParent> ParentsQueryable<TParent>(IMongoQueryable<TChild> children, IClientSessionHandle session = null, AggregateOptions options = null) where TParent : IEntity
        {
            if (typeof(TParent) == typeof(TChild)) throw new InvalidOperationException("Both parent and child types cannot be the same");

            if (isInverse)
            {
                return children
                        .Join(
                            JoinQueryable(session, options),
                            c => c.ID,
                            j => j.ParentID,
                            (_, j) => j)
                        .Join(
                            DB.Collection<TParent>(),
                            j => j.ChildID,
                            p => p.ID,
                            (_, p) => p)
                        .Distinct();
            }
            else
            {
                return children
                       .Join(
                            JoinQueryable(session, options),
                            c => c.ID,
                            j => j.ChildID,
                            (_, j) => j)
                       .Join(
                            DB.Collection<TParent>(),
                            j => j.ParentID,
                            p => p.ID,
                            (_, p) => p)
                       .Distinct();
            }
        }

        /// <summary>
        /// An IQueryable of child Entities for the parent.
        /// </summary>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IMongoQueryable<TChild> ChildrenQueryable(IClientSessionHandle session = null, AggregateOptions options = null)
        {
            parent.ThrowIfUnsaved();

            if (isInverse)
            {
                return JoinQueryable(session, options)
                       .Where(j => j.ChildID == parent.ID)
                       .Join(
                           DB.Collection<TChild>(),
                           j => j.ParentID,
                           c => c.ID,
                           (_, c) => c);
            }
            else
            {
                return JoinQueryable(session, options)
                       .Where(j => j.ParentID == parent.ID)
                       .Join(
                           DB.Collection<TChild>(),
                           j => j.ChildID,
                           c => c.ID,
                           (_, c) => c);
            }
        }

        /// <inheritdoc/>
        public IEnumerator<TChild> GetEnumerator() => ChildrenQueryable().GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => ChildrenQueryable().GetEnumerator();

    }
}
