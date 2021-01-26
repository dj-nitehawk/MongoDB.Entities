using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents a transaction used to carry out inter-related write operations.
    /// <para>TIP: Remember to always call .Dispose() after use or enclose in a 'Using' statement.</para>
    /// <para>IMPORTANT: Use the methods on this transaction to perform operations and not the methods on the DB class.</para>
    /// </summary>
    public class Transaction : DBContext, IDisposable
    {
        private readonly IMongoClient client;

        /// <summary>
        /// Instantiates and begins a transaction.
        /// </summary>
        /// <param name="database">The name of the database to use for this transaction. default db is used if not specified</param>
        /// <param name="options">Client session options for this transaction</param>
        public Transaction(string database = default, ClientSessionOptions options = null)
        {
            client = DB.Database(database).Client;
            Session = client.StartSession(options);
            Session.StartTransaction();
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

        #region IDisposable Support

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Session.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion        
    }
}
