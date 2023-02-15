using System;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities;

public sealed partial class Many<TChild, TParent> where TChild : IEntity where TParent : IEntity
{
    /// <summary>
    /// Adds a new child reference.
    /// <para>WARNING: Make sure to save the parent and child Entities before calling this method.</para>
    /// </summary>
    /// <param name="child">The child Entity to add.</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task AddAsync(TChild child, IClientSessionHandle? session = null, CancellationToken cancellation = default)
    {
        return AddAsync(child.GetId(), session, cancellation);
    }

    /// <summary>
    /// Adds multiple child references in a single bulk operation
    /// <para>WARNING: Make sure to save the parent and child Entities before calling this method.</para>
    /// </summary>
    /// <param name="children">The child Entities to add</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task AddAsync(IEnumerable<TChild> children, IClientSessionHandle? session = null, CancellationToken cancellation = default)
    {
        return AddAsync(children.Select(Cache<TChild>.SelectIdFunc()), session, cancellation);
    }

    /// <summary>
    /// Adds a new child reference.
    /// <para>WARNING: Make sure to save the parent and child Entities before calling this method.</para>
    /// </summary>
    /// <param name="childID">The ID of the child Entity to add.</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task AddAsync(object? childID, IClientSessionHandle? session = null, CancellationToken cancellation = default)
    {
        return AddAsync(new[] { childID }, session, cancellation);
    }

    /// <summary>
    /// Adds multiple child references in a single bulk operation
    /// <para>WARNING: Make sure to save the parent and child Entities before calling this method.</para>
    /// </summary>
    /// <param name="childIDs">The IDs of the child Entities to add.</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task AddAsync(IEnumerable<object?> childIDs, IClientSessionHandle? session = null, CancellationToken cancellation = default)
    {
        parent.ThrowIfUnsaved();

        var models = new List<WriteModel<JoinRecord>>(childIDs.Count());
        foreach (var cid in childIDs)
        {
            cid.ThrowIfUnsaved();
            var parentID = isInverse ? cid : parent.GetId();
            var childID = isInverse ? parent.GetId() : cid;

            var filter = Builders<JoinRecord>.Filter.Where(
                j => j.ParentID == parentID &&
                j.ChildID == childID);

            var update = Builders<JoinRecord>.Update
                .Set(j => j.ParentID, parentID)
                .Set(j => j.ChildID, childID);

            models.Add(new UpdateOneModel<JoinRecord>(filter, update) { IsUpsert = true });
        }

        return session == null
               ? JoinCollection.BulkWriteAsync(models, unOrdBlkOpts, cancellation)
               : JoinCollection.BulkWriteAsync(session, models, unOrdBlkOpts, cancellation);
    }
}
