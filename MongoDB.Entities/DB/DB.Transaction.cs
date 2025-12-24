using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB
{
    /// <summary>
    /// Returns the session object used for transactions
    /// </summary>
    public IClientSessionHandle? Session { get; protected set; }

    /// <summary>
    /// Starts a transaction and returns a session object.
    /// <para>
    /// WARNING: Only one transaction is allowed per DB instance.
    /// Call <c>db.Session.Dispose()</c> and assign a null to it before calling this method a second time.
    /// Trying to start a second transaction for this DB instance will throw an exception.
    /// </para>
    /// </summary>
    /// <param name="options">Client session options for this transaction</param>
    public IClientSessionHandle Transaction(ClientSessionOptions? options = null)
    {
        if (Session is not null)
            throw new NotSupportedException("Only one transaction is allowed per DB instance. Dispose and nullify the Session before calling this method again!");

        Session = _mongoDb.Client.StartSession(options);
        Session.StartTransaction();

        return Session;
    }

    /// <summary>
    /// Commits a transaction to MongoDB
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task CommitAsync(CancellationToken cancellation = default)
        => Session?.CommitTransactionAsync(cancellation) ?? throw new InvalidOperationException("Transaction has not been started yet!");

    /// <summary>
    /// Aborts and rolls back a transaction
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task AbortAsync(CancellationToken cancellation = default)
        => Session?.AbortTransactionAsync(cancellation) ?? throw new InvalidOperationException("Transaction has not been started yet!");
}