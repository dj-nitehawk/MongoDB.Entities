using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB
{
    protected internal IClientSessionHandle? SessionHandle { get; set; }

    /// <summary>
    /// Creates a <see cref="Transaction" /> instance (from the current DB instance) for performing operations in an atomic manner within a transaction.
    /// </summary>
    /// <param name="options">Client session options for this transaction</param>
    /// <param name="transactionOptions">options for the transaction</param>
    public Transaction Transaction(ClientSessionOptions? options = null, TransactionOptions? transactionOptions = null)
        => new(this, options, transactionOptions);
}