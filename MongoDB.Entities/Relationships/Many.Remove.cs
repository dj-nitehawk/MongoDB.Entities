using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public sealed partial class Many<TChild> where TChild : IEntity
    {
        /// <summary>
        /// Removes a child reference.
        /// </summary>
        /// <param name="child">The child IEntity to remove the reference of.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task RemoveAsync(TChild child, IClientSessionHandle session = null, CancellationToken cancellation = default)
        {
            return RemoveAsync(child.ID, session, cancellation);
        }

        /// <summary>
        /// Removes a child reference.
        /// </summary>
        /// <param name="childID">The ID of the child Entity to remove the reference of.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task RemoveAsync(string childID, IClientSessionHandle session = null, CancellationToken cancellation = default)
        {
            return RemoveAsync(new[] { childID }, session, cancellation);
        }

        /// <summary>
        /// Removes child references.
        /// </summary>
        /// <param name="children">The child Entities to remove the references of.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task RemoveAsync(IEnumerable<TChild> children, IClientSessionHandle session = null, CancellationToken cancellation = default)
        {
            return RemoveAsync(children.Select(c => c.ID), session, cancellation);
        }

        /// <summary>
        /// Removes child references.
        /// </summary>
        /// <param name="childIDs">The IDs of the child Entities to remove the references of</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task RemoveAsync(IEnumerable<string> childIDs, IClientSessionHandle session = null, CancellationToken cancellation = default)
        {
            var filter =
                isInverse
                ? Builders<JoinRecord>.Filter.And(
                    Builders<JoinRecord>.Filter.Eq(j => j.ChildID, parent.ID),
                    Builders<JoinRecord>.Filter.In(j => j.ParentID, childIDs))

                : Builders<JoinRecord>.Filter.And(
                    Builders<JoinRecord>.Filter.Eq(j => j.ParentID, parent.ID),
                    Builders<JoinRecord>.Filter.In(j => j.ChildID, childIDs));

            return session == null
                   ? JoinCollection.DeleteOneAsync(filter, null, cancellation)
                   : JoinCollection.DeleteOneAsync(session, filter, null, cancellation);
        }
    }
}
