using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DBContext
    {
        /// <summary>
        /// Starts a transaction and returns a session object.
        /// <para>WARNING: Only one transaction is allowed per DBContext instance. 
        /// Call Session.Dispose() and assign a null to it before calling this method a second time. 
        /// Trying to start a second transaction for this DBContext instance will throw an exception.</para>
        /// </summary>
        /// <param name="options">Client session options for this transaction</param>
        public IClientSessionHandle Transaction(ClientSessionOptions? options = null)
        {
            if (Session is null)
            {
                Session = Client.StartSession(options);
                Session.StartTransaction();
                return Session;
            }

            throw new NotSupportedException(
                "Only one transaction is allowed per DBContext instance. Dispose and nullify the Session before calling this method again!");
        }


        /// <summary>
        /// Commits a transaction to MongoDB
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task CommitAsync(CancellationToken cancellation = default)
        {
            if (Session is null)
            {
                throw new ArgumentNullException(nameof(Session), "Please call Transaction<T>() first before committing");
            }

            return Session.CommitTransactionAsync(cancellation);
        }

        /// <summary>
        /// Aborts and rolls back a transaction
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task AbortAsync(CancellationToken cancellation = default)
        {
            if (Session is null)
            {
                throw new ArgumentNullException(nameof(Session), "Please call Transaction<T>() first before aborting");
            }

            return Session.AbortTransactionAsync(cancellation);
        }
    }
}
