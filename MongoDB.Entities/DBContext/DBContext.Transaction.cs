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
        /// <param name="database">The name of the database to use for this transaction. default db is used if not specified</param>
        /// <param name="options">Client session options for this transaction</param>
        public IClientSessionHandle Transaction(string database = default, ClientSessionOptions options = null)
        {
            if (Session is null)
            {
                Session = DB.Database(database).Client.StartSession(options);
                Session.StartTransaction();
                return Session;
            }

            throw new NotSupportedException(
                "Only one transaction is allowed per DBContext instance. Dispose and nullify the Session before calling this method again!");
        }

        /// <summary>
        /// Starts a transaction and returns a session object for a given entity type.
        /// <para>WARNING: Only one transaction is allowed per DBContext instance. 
        /// Call Session.Dispose() and assign a null to it before calling this method a second time. 
        /// Trying to start a second transaction for this DBContext instance will throw an exception.</para>
        /// </summary>
        /// <typeparam name="T">The entity type to determine the database from for the transaction</typeparam>
        /// <param name="options">Client session options (not required)</param>
        public IClientSessionHandle Transaction<T>(ClientSessionOptions options = null) where T : IEntity
        {
            return Transaction(DB.DatabaseName<T>(tenantPrefix), options);
        }

        /// <summary>
        /// Commits a transaction to MongoDB
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task CommitAsync(CancellationToken cancellation = default) => Session.CommitTransactionAsync(cancellation);

        /// <summary>
        /// Aborts and rolls back a transaction
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task AbortAsync(CancellationToken cancellation = default) => Session.AbortTransactionAsync(cancellation);
    }
}
