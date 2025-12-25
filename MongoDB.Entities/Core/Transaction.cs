using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

/// <summary>
/// Represents a DB instance that has a transaction started, which can be used to carry out inter-related write operations atomically.
/// <para>TIP: Remember to always call .Dispose() after use or enclose in a 'Using' statement.</para>
/// </summary>
public class Transaction : DB, IDisposable
{
    /// <summary>
    /// The client session handle for this transaction instance.
    /// </summary>
    public IClientSessionHandle Session => SessionHandle ?? throw new InvalidOperationException("The session hasn't been started yet!");

    internal Transaction(DB source, ClientSessionOptions? sessionOpts = null, TransactionOptions? trnsOpts = null) : base(source)
    {
        SessionHandle = Database().Client.StartSession(sessionOpts);
        SessionHandle.StartTransaction(trnsOpts);
    }

    /// <summary>
    /// Commits a transaction to MongoDB
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task CommitAsync(CancellationToken cancellation = default)
        => SessionHandle?.CommitTransactionAsync(cancellation) ?? throw new InvalidOperationException("Transaction has not been started yet!");

    /// <summary>
    /// Aborts and rolls back a transaction
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task AbortAsync(CancellationToken cancellation = default)
        => SessionHandle?.AbortTransactionAsync(cancellation) ?? throw new InvalidOperationException("Transaction has not been started yet!");

    public void Dispose()
    {
        if (SessionHandle is null)
            return;

        SessionHandle.Dispose();
        SessionHandle = null;
    }
}