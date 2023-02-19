using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace MongoDB.Entities;

public sealed partial class Many<TChild, TParent> where TChild : IEntity where TParent : IEntity
{
    /// <summary>
    /// An IAggregateFluent of JoinRecords for this relationship
    /// </summary>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="options">An optional AggregateOptions object</param>
    public IAggregateFluent<JoinRecord> JoinFluent(IClientSessionHandle? session = null, AggregateOptions? options = null)
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
    public IAggregateFluent<TParent> ParentsFluent(IAggregateFluent<TChild> children)
    {
        return typeof(TParent) == typeof(TChild)
            ? throw new InvalidOperationException("Both parent and child types cannot be the same")
            : isInverse
            ? children
                   .Lookup<TChild, JoinRecord, Joined<JoinRecord>>(
                        JoinCollection,
                        Cache<TChild>.IdExpression,
                        r => r.ParentID,
                        j => j.Results)
                   .ReplaceRoot(j => j.Results[0])
                   .Lookup<JoinRecord, TParent, Joined<TParent>>(
                        DB.Collection<TParent>(),
                        r => r.ChildID,
                        Cache<TParent>.IdExpression,
                        j => j.Results)
                   .ReplaceRoot(j => j.Results[0])
                   .Distinct()
            : children
                   .Lookup<TChild, JoinRecord, Joined<JoinRecord>>(
                        JoinCollection,
                        Cache<TChild>.IdExpression,
                        r => r.ChildID,
                        j => j.Results)
                   .ReplaceRoot(j => j.Results[0])
                   .Lookup<JoinRecord, TParent, Joined<TParent>>(
                        DB.Collection<TParent>(),
                        r => r.ParentID,
                        Cache<TParent>.IdExpression,
                        j => j.Results)
                   .ReplaceRoot(j => j.Results[0])
                   .Distinct();
    }

    /// <summary>
    /// Get an IAggregateFluent of parents matching a single child ID for this relationship.
    /// </summary>
    /// <typeparam name="TParent">The type of the parent IEntity</typeparam>
    /// <param name="childID">An child ID</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="options">An optional AggregateOptions object</param>
    public IAggregateFluent<TParent> ParentsFluent(string? childID, IClientSessionHandle? session = null, AggregateOptions? options = null)
    {
        return ParentsFluent(new[] { childID }, session, options);
    }

    /// <summary>
    /// Get an IAggregateFluent of parents matching multiple child IDs for this relationship.
    /// </summary>
    /// <typeparam name="TParent">The type of the parent IEntity</typeparam>
    /// <param name="childIDs">An IEnumerable of child IDs</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="options">An optional AggregateOptions object</param>
    public IAggregateFluent<TParent> ParentsFluent(IEnumerable<object?> childIDs, IClientSessionHandle? session = null, AggregateOptions? options = null)
    {
        return typeof(TParent) == typeof(TChild)
            ? throw new InvalidOperationException("Both parent and child types cannot be the same")
            : isInverse
            ? JoinFluent(session, options)
                   .Match(f => f.In(j => j.ParentID, childIDs))
                   .Lookup<JoinRecord, TParent, Joined<TParent>>(
                        DB.Collection<TParent>(),
                        j => j.ChildID,
                        Cache<TParent>.IdExpression,
                        j => j.Results)
                   .ReplaceRoot(j => j.Results[0])
                   .Distinct()
            : JoinFluent(session, options)
                   .Match(f => f.In(j => j.ChildID, childIDs))
                   .Lookup<JoinRecord, TParent, Joined<TParent>>(
                        DB.Collection<TParent>(),
                        r => r.ParentID,
                        Cache<TParent>.IdExpression,
                        j => j.Results)
                   .ReplaceRoot(j => j.Results[0])
                   .Distinct();
    }

    /// <summary>
    /// An IAggregateFluent of child Entities for the parent.
    /// </summary>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="options">An optional AggregateOptions object</param>
    public IAggregateFluent<TChild> ChildrenFluent(IClientSessionHandle? session = null, AggregateOptions? options = null)
    {
        parent.ThrowIfUnsaved();

        return isInverse
            ? JoinFluent(session, options)
                    .Match(f => f.Eq(r => r.ChildID, parent.GetId()))
                    .Lookup<JoinRecord, TChild, Joined<TChild>>(
                        DB.Collection<TChild>(),
                        r => r.ParentID,
                        Cache<TChild>.IdExpression,
                        j => j.Results)
                    .ReplaceRoot(j => j.Results[0])
            : JoinFluent(session, options)
                    .Match(f => f.Eq(r => r.ParentID, parent.GetId()))
                    .Lookup<JoinRecord, TChild, Joined<TChild>>(
                        DB.Collection<TChild>(),
                        r => r.ChildID,
                        Cache<TChild>.IdExpression,
                        j => j.Results)
                    .ReplaceRoot(j => j.Results[0]);
    }

    private class Joined<T> : JoinRecord
    {
        public T[] Results { get; set; } = null!;
    }
}
