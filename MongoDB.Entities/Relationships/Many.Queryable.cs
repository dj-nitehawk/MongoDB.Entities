using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MongoDB.Entities;

public sealed partial class Many<TChild, TParent> : IEnumerable<TChild> where TChild : IEntity where TParent : IEntity
{
    /// <summary>
    /// An IQueryable of JoinRecords for this relationship
    /// </summary>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="options">An optional AggregateOptions object</param>
    public IQueryable<JoinRecord> JoinQueryable(IClientSessionHandle? session = null, AggregateOptions? options = null)
        => session == null
               ? JoinCollection.AsQueryable(options)
               : JoinCollection.AsQueryable(session, options);

    /// <summary>
    /// Get an IQueryable of parents matching a single child ID for this relationship.
    /// </summary>
    /// <param name="childID">A child ID</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="options">An optional AggregateOptions object</param>
    public IQueryable<TParent> ParentsQueryable(string childID, IClientSessionHandle? session = null, AggregateOptions? options = null)
        => ParentsQueryable([childID], session, options);

    /// <summary>
    /// Get an IQueryable of parents matching multiple child IDs for this relationship.
    /// </summary>
    /// <param name="childIDs">An IEnumerable of child IDs</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="options">An optional AggregateOptions object</param>
    public IQueryable<TParent> ParentsQueryable(IEnumerable<string> childIDs, IClientSessionHandle? session = null, AggregateOptions? options = null)
    {
        return typeof(TParent) == typeof(TChild)
                   ? throw new InvalidOperationException("Both parent and child types cannot be the same")
                   : _isInverse
                       ? JoinQueryable(session, options)
                         .Where(j => childIDs.Contains(j.ParentID))
                         .Join(
                             _dbInstance.Queryable<TParent>(),
                             j => j.ChildID,
                             Cache<TParent>.IdExpression,
                             (_, p) => p)
                         .Distinct()
                       : JoinQueryable(session, options)
                         .Where(j => childIDs.Contains(j.ChildID))
                         .Join(
                             _dbInstance.Queryable<TParent>(),
                             j => j.ParentID,
                             Cache<TParent>.IdExpression,
                             (_, p) => p)
                         .Distinct();
    }

    /// <summary>
    /// An IQueryable of child Entities for the parent.
    /// </summary>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="options">An optional AggregateOptions object</param>
    public IQueryable<TChild> ChildrenQueryable(IClientSessionHandle? session = null, AggregateOptions? options = null)
    {
        _parent.ThrowIfUnsaved();

        return _isInverse
                   ? JoinQueryable(session, options)
                     .Where(j => Equals(j.ChildID, _parent.GetId()))
                     .Join(
                         _dbInstance.Collection<TChild>(),
                         j => j.ParentID,
                         Cache<TChild>.IdExpression,
                         (_, c) => c)
                   : JoinQueryable(session, options)
                     .Where(j => Equals(j.ParentID, _parent.GetId()))
                     .Join(
                         _dbInstance.Collection<TChild>(),
                         j => j.ChildID,
                         Cache<TChild>.IdExpression,
                         (_, c) => c);
    }

    /// <inheritdoc />
    public IEnumerator<TChild> GetEnumerator()
        => ChildrenQueryable().GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
        => ChildrenQueryable().GetEnumerator();
}