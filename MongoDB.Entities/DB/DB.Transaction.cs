using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB
{
    protected IClientSessionHandle? SessionHandle { get; set; }

    /// <summary>
    /// Creates a <see cref="Transaction" /> instance (from the current DB instance) for performing operations in an atomic manner within a transaction.
    /// </summary>
    /// <remarks>Retains derived save/update hooks and binds hook operations to the new transaction session.</remarks>
    /// <param name="options">Client session options for this transaction</param>
    /// <param name="transactionOptions">options for the transaction</param>
    public Transaction Transaction(ClientSessionOptions? options = null, TransactionOptions? transactionOptions = null)
    {
        var transaction = new Transaction(this, options, transactionOptions);
        // Capture overrides privately without making the source another transaction session owner.
        if (GetType() != typeof(DB) && GetType() != typeof(Transaction))
            transaction._hookContext = CloneHookContext();

        return transaction;
    }
}