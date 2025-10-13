using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public static partial class DB
{
    /// <summary>
    /// Gets the DBInstance for a given IEntity type.
    /// <para>TIP: Try never to use this unless really necessary.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public static DBInstance DbInstance<T>() where T : IEntity
        => Cache<T>.DbInstance;

    /// <summary>
    /// Gets the Default DBInstance for DB.
    /// </summary>
    public static DBInstance DbInstance()
        => _defaultDbInstance ?? throw new InvalidOperationException("Default DBInstance is not set. Please call DB.Init() first.");

    /// <summary>
    /// Gets the Default DBInstance for DB.
    /// </summary>
    public static DBInstance DbInstance(string? dbName)
    {
        var dbInstance = DBInstance.Instance(dbName);

        if (dbInstance == null && string.IsNullOrEmpty(dbName))
            dbInstance = _defaultDbInstance;
                             
        if (dbInstance == null)     
            throw new InvalidOperationException($"Database connection is not initialized for [{(string.IsNullOrEmpty(dbName) ? "Default" : dbName)}]");
        
        return dbInstance;
    }
    

}