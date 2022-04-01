namespace MongoDB.Entities;

/// <summary>
/// Represents a transaction used to carry out inter-related write operations.
/// <para>TIP: Remember to always call .Dispose() after use or enclose in a 'Using' statement.</para>
/// <para>IMPORTANT: Use the methods on this transaction to perform operations and not the methods on the DB class.</para>
/// </summary>
public class Transaction : DBContext, IDisposable
{
    /// <summary>
    /// Instantiates and begins a transaction.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="database">The name of the database to use for this transaction. default db is used if not specified</param>
    /// <param name="options">Client session options for this transaction</param>    
    public Transaction(MongoServerContext context, string database, ClientSessionOptions? options = null) : base(mongoContext: new(context), database: database)
    {
        MongoServerContext.Transaction(options);
    }

    /// <summary>
    /// Instantiates and begins a transaction.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="database">The name of the database to use for this transaction. default db is used if not specified</param>
    /// <param name="options">Client session options for this transaction</param>    
    public Transaction(MongoServerContext context, IMongoDatabase database, ClientSessionOptions? options = null) : base(mongoContext: new(context), database: database)
    {
        MongoServerContext.Transaction(options);
    }

    #region IDisposable Support

    private bool _disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                MongoServerContext.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }

    #endregion
}
