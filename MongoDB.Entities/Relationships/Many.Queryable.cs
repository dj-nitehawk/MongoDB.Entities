using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Entities;

public sealed partial class Many<TChild, TParent> : IEnumerable<TChild> where TChild : IEntity where TParent : IEntity
{
    /// <summary>
    /// An IQueryable of JoinRecords for this relationship
    /// </summary>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="options">An optional AggregateOptions object</param>
    public IMongoQueryable<JoinRecord> JoinQueryable(IClientSessionHandle? session = null, AggregateOptions? options = null)
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
    public IMongoQueryable<TParent> ParentsQueryable(string childID, IClientSessionHandle? session = null, AggregateOptions? options = null)
    {
        return ParentsQueryable(new[] { childID }, session, options);
    }

    /// <summary>
    /// Get an IQueryable of parents matching multiple child IDs for this relationship.
    /// </summary>
    /// <typeparam name="TParent">The type of the parent IEntity</typeparam>
    /// <param name="childIDs">An IEnumerable of child IDs</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="options">An optional AggregateOptions object</param>
    public IMongoQueryable<TParent> ParentsQueryable(IEnumerable<string> childIDs, IClientSessionHandle? session = null, AggregateOptions? options = null)
    {
        return typeof(TParent) == typeof(TChild)
            ? throw new InvalidOperationException("Both parent and child types cannot be the same")
            : isInverse
            ? JoinQueryable(session, options)
                   .Where(j => childIDs.Contains(j.ParentID))
                   .Join(
                       DB.Queryable<TParent>(),
                       j => j.ChildID,
                       Cache<TParent>.SelectIdExpression(),
                       (_, p) => p)
                   .Distinct()
            : JoinQueryable(session, options)
                   .Where(j => childIDs.Contains(j.ChildID))
                   .Join(
                       DB.Queryable<TParent>(),
                       j => j.ParentID,
                       Cache<TParent>.SelectIdExpression(),
                       (_, p) => p)
                   .Distinct();
    }

    /// <summary>
    /// Get an IQueryable of parents matching a supplied IQueryable of children for this relationship.
    /// </summary>
    /// <typeparam name="TParent">The type of the parent IEntity</typeparam>
    /// <param name="children">An IQueryable of children</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="options">An optional AggregateOptions object</param>
    [Obsolete("This method is no longer supported due to incompatibilities with LINQ3 translation engine!", true)]
    public IMongoQueryable<TParent> ParentsQueryable(IMongoQueryable<TChild> children, IClientSessionHandle? session = null, AggregateOptions? options = null)
    {
        throw new NotSupportedException();
        //return typeof(TParent) == typeof(TChild)
        //    ? throw new InvalidOperationException("Both parent and child types cannot be the same")
        //    : isInverse
        //    ? children
        //            .Join(
        //                JoinQueryable(session, options),
        //                c => c.ID,
        //                j => j.ParentID,
        //                (_, j) => j)
        //            .Join(
        //                DB.Queryable<TParent>(),
        //                j => j.ChildID,
        //                p => p.ID,
        //                (_, p) => p)
        //            .Distinct()
        //    : children
        //           .Join(
        //                JoinQueryable(session, options),
        //                c => c.ID,
        //                j => j.ChildID,
        //                (_, j) => j)
        //           .Join(
        //                DB.Queryable<TParent>(),
        //                j => j.ParentID,
        //                p => p.ID,
        //                (_, p) => p)
        //           .Distinct();
    }

    /// <summary>
    /// An IQueryable of child Entities for the parent.
    /// </summary>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="options">An optional AggregateOptions object</param>
    public IMongoQueryable<TChild> ChildrenQueryable(IClientSessionHandle? session = null, AggregateOptions? options = null)
    {
        parent.ThrowIfUnsaved();

        return isInverse
            ? JoinQueryable(session, options)
                   .Where(j => Equals(j.ChildID,parent.GetId()))
                   .Join(
                       DB.Collection<TChild>(),
                       j => j.ParentID,
                       Cache<TChild>.SelectIdExpression(),
                       (_, c) => c)
            : JoinQueryable(session, options)
                   .Where(j => Equals(j.ParentID,parent.GetId()))
                   .Join(
                       DB.Collection<TChild>(),
                       j => j.ChildID,
                       Cache<TChild>.SelectIdExpression(),
                       (_, c) => c);
    }

    /// <inheritdoc/>
    public IEnumerator<TChild> GetEnumerator() => ChildrenQueryable().GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => ChildrenQueryable().GetEnumerator();

}
