using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB
{
    /// <summary>
    /// Gets a transaction context/scope for a given database or the default database if not specified.
    /// </summary>
    /// <param name="database">The name of the database which this transaction is for (not required)</param>
    /// <param name="options">Client session options (not required)</param>
    /// <param name="modifiedBy"></param>
    public Transaction Transaction(string? database = null, ClientSessionOptions? options = null, ModifiedBy? modifiedBy = null)
        => new(database, options, modifiedBy);

    /// <summary>
    /// Gets a transaction context/scope for a given entity type's database
    /// </summary>
    /// <typeparam name="T">The entity type to determine the database from for the transaction</typeparam>
    /// <param name="options">Client session options (not required)</param>
    /// <param name="modifiedBy"></param>
    public Transaction Transaction<T>(ClientSessionOptions? options = null, ModifiedBy? modifiedBy = null) where T : IEntity
        => new(DatabaseName<T>(), options, modifiedBy);
}