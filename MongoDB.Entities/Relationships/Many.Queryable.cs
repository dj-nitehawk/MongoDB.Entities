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
    public IQueryable<TParent> ParentsQueryable(object childID, IClientSessionHandle? session = null, AggregateOptions? options = null)
        => ParentsQueryable([childID], session, options);

    /// <summary>
    /// Get an IQueryable of parents matching multiple child IDs for this relationship.
    /// </summary>
    /// <param name="childIDs">An IEnumerable of child IDs</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="options">An optional AggregateOptions object</param>
    public IQueryable<TParent> ParentsQueryable(IEnumerable<object?> childIDs, IClientSessionHandle? session = null, AggregateOptions? options = null)
        => ParentsQueryableByIds(childIDs, session, options);

    /// <summary>
    /// Get an IQueryable of parents matching multiple child IDs of any CLR type
    /// (including value types such as Guid, long, and ObjectId) for this relationship.
    /// </summary>
    /// <typeparam name="TId">The CLR type of the child IDs</typeparam>
    /// <param name="childIDs">An IEnumerable of child IDs</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="options">An optional AggregateOptions object</param>
    public IQueryable<TParent> ParentsQueryable<TId>(IReadOnlyList<TId> childIDs, IClientSessionHandle? session = null, AggregateOptions? options = null) where TId : struct
        => ParentsQueryableByIds(BoxIds(childIDs), session, options);

    IQueryable<TParent> ParentsQueryableByIds(IEnumerable<object?> childIDs, IClientSessionHandle? session, AggregateOptions? options)
    {
        var childIds = (childIDs as object?[] ?? childIDs.ToArray()).Select(Cache<TChild>.IdToBsonValue).ToArray();

        return typeof(TParent) == typeof(TChild)
                   ? throw new InvalidOperationException("Both parent and child types cannot be the same")
                   : _isInverse
                       ? JoinQueryable(session, options)
                         .Where(j => childIds.Contains(j.ParentID))
                         .Join(
                             _db.Queryable<TParent>(),
                             j => j.ChildID,
                             Cache<TParent>.BsonValueIdExpression,
                             (_, p) => p)
                         .Distinct()
                       : JoinQueryable(session, options)
                         .Where(j => childIds.Contains(j.ChildID))
                         .Join(
                             _db.Queryable<TParent>(),
                             j => j.ParentID,
                             Cache<TParent>.BsonValueIdExpression,
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

        var parentId = _parent.GetBsonId();

        return _isInverse
                   ? JoinQueryable(session, options)
                     .Where(j => j.ChildID == parentId)
                     .Join(
                         _db.Collection<TChild>(),
                         j => j.ParentID,
                         Cache<TChild>.BsonValueIdExpression,
                         (_, c) => c)
                   : JoinQueryable(session, options)
                     .Where(j => j.ParentID == parentId)
                     .Join(
                         _db.Collection<TChild>(),
                         j => j.ChildID,
                         Cache<TChild>.BsonValueIdExpression,
                         (_, c) => c);
    }

    /// <inheritdoc />
    public IEnumerator<TChild> GetEnumerator()
        => ChildrenQueryable().GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
        => ChildrenQueryable().GetEnumerator();
}
