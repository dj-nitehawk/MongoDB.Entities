using MongoDB.Driver;
using System;

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
    /// <param name="database">The name of the database to use for this transaction. default db is used if not specified</param>
    /// <param name="options">Client session options for this transaction</param>
    /// <param name="modifiedBy">An optional ModifiedBy instance. 
    /// When supplied, all save/update operations performed via this DBContext instance will set the value on entities that has a property of type ModifiedBy. 
    /// You can inherit from the ModifiedBy class and add your own properties to it. 
    /// Only one ModifiedBy property is allowed on a single entity type.</param>
    public Transaction(string? database = null, ClientSessionOptions? options = null, ModifiedBy? modifiedBy = null)
    {
        Session = DB.Database(database).Client.StartSession(options);
        Session.StartTransaction();
        ModifiedBy = modifiedBy;
    }

    #region IDisposable Support

    bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (disposedValue)
            return;

        if (disposing)
            Session?.Dispose();

        disposedValue = true;
    }

    public void Dispose()
    {
        Dispose(true);
    }

    #endregion        
}