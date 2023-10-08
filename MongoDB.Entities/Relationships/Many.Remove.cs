using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities;

public sealed partial class Many<TChild, TParent> where TChild : IEntity where TParent : IEntity
{
    /// <summary>
    /// Removes a child reference.
    /// </summary>
    /// <param name="child">The child IEntity to remove the reference of.</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task RemoveAsync(TChild child, IClientSessionHandle? session = null, CancellationToken cancellation = default)
    {
        return RemoveAsync(child.GetId(), session, cancellation);
    }

    /// <summary>
    /// Removes a child reference.
    /// </summary>
    /// <param name="childID">The ID of the child Entity to remove the reference of.</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task RemoveAsync(object childID, IClientSessionHandle? session = null, CancellationToken cancellation = default)
    {
        return RemoveAsync(new[] { childID }, session, cancellation);
    }

    /// <summary>
    /// Removes child references.
    /// </summary>
    /// <param name="children">The child Entities to remove the references of.</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task RemoveAsync(IEnumerable<TChild> children, IClientSessionHandle? session = null, CancellationToken cancellation = default)
    {
        return RemoveAsync(children.Select(Cache<TChild>.IdSelector), session, cancellation);
    }

    /// <summary>
    /// Removes child references.
    /// </summary>
    /// <param name="childIDs">The IDs of the child Entities to remove the references of</param>
    /// <param name="session">An optional session if using within a transaction</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task RemoveAsync(IEnumerable<object> childIDs, IClientSessionHandle? session = null, CancellationToken cancellation = default)
    {
        var filter =
            _isInverse
            ? Builders<JoinRecord>.Filter.And(
                Builders<JoinRecord>.Filter.Eq(j => j.ChildID, _parent.GetId()),
                Builders<JoinRecord>.Filter.In(j => j.ParentID, childIDs))

            : Builders<JoinRecord>.Filter.And(
                Builders<JoinRecord>.Filter.Eq(j => j.ParentID, _parent.GetId()),
                Builders<JoinRecord>.Filter.In(j => j.ChildID, childIDs));

        return session == null
               ? JoinCollection.DeleteManyAsync(filter, null, cancellation)
               : JoinCollection.DeleteManyAsync(session, filter, null, cancellation);
    }
}
