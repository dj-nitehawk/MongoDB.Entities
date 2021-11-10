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
            return MongoServerContext.Transaction(options);
        }

        /// <summary>
        /// Creates a new DBContext and a new MongoServerContext and Starts a transaction on the new instance.        
        /// </summary>
        /// <param name="options">Client session options for this transaction</param>
        public DBContext TransactionCopy(ClientSessionOptions? options = null)
        {
            var server = new MongoServerContext(MongoServerContext);
            server.Transaction(options);
            return new DBContext(server, Database, Options);
        }

        /// <summary>
        /// Commits a transaction to MongoDB
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task CommitAsync(CancellationToken cancellation = default)
        {
            return MongoServerContext.CommitAsync(cancellation);
        }

        /// <summary>
        /// Aborts and rolls back a transaction
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task AbortAsync(CancellationToken cancellation = default)
        {
            return MongoServerContext.AbortAsync(cancellation);
        }
    }
}
