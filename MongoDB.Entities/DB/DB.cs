using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
/// <summary>
/// The main entrypoint for all data access methods of the library
/// </summary>
public static partial class DB
{
    internal static IServiceProvider? ServiceProvider { get; private set; }
    internal static event Action? DefaultDbChanged;

    static DBInstance? _defaultDbInstance;

    /// <summary>
    /// Initializes a MongoDB connection with the given connection parameters.
    /// <para>WARNING: will throw an error if server is not reachable!</para>
    /// You can call this method as many times as you want (such as in serverless functions) with the same parameters and the connections won't get
    /// duplicated.
    /// </summary>
    /// <param name="database">Name of the database</param>
    /// <param name="host">Address of the MongoDB server</param>
    /// <param name="port">Port number of the server</param>
    public static async Task InitAsync(string database, string host = "127.0.0.1", int port = 27017)
    {
        var dbInstance = await DBInstance.InitAsync(database, host, port);
        _defaultDbInstance ??= dbInstance;
    }

    /// <summary>
    /// Initializes a MongoDB connection with the given connection parameters.
    /// <para>WARNING: will throw an error if server is not reachable!</para>
    /// You can call this method as many times as you want (such as in serverless functions) with the same parameters and the connections won't get
    /// duplicated.
    /// </summary>
    /// <param name="database">Name of the database</param>
    /// <param name="settings">A MongoClientSettings object</param>
    public static async Task InitAsync(string database, MongoClientSettings settings)
    {
        var dbInstance = await DBInstance.InitAsync(database, settings);
        _defaultDbInstance ??= dbInstance;
    }
    
    /// <summary>
    /// Gets a list of all database names from the server
    /// </summary>
    /// <param name="host">Address of the MongoDB server</param>
    /// <param name="port">Port number of the server</param>
    public static Task<IEnumerable<string>> AllDatabaseNamesAsync(string host = "127.0.0.1", int port = 27017)
        => AllDatabaseNamesAsync(new() { Server = new(host, port) });

    /// <summary>
    /// Gets a list of all database names from the server
    /// </summary>
    /// <param name="settings">A MongoClientSettings object</param>
    public static async Task<IEnumerable<string>> AllDatabaseNamesAsync(MongoClientSettings settings)
        => await (await new MongoClient(settings).ListDatabaseNamesAsync().ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);

    /// <summary>
    /// Specifies the database that a given entity type should be stored in.
    /// Only needed for entity types you want stored in a db other than the default db.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="database">The name of the database</param>
    public static void DatabaseFor<T>(string database) where T : IEntity
    {
        TypeMap.AddDbInstanceMapping(typeof(T),  DbInstance(database));
    }

    /// <summary>
    /// Gets the IMongoDatabase for the given entity type
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    public static IMongoDatabase Database<T>() where T : IEntity
        => Cache<T>.Database;

    /// <summary>
    /// Gets the IMongoDatabase for a given database name if it has been previously initialized.
    /// You can also get the default database by passing 'default' or 'null' for the name parameter.
    /// </summary>
    /// <param name="name">The name of the database to retrieve</param>
    public static IMongoDatabase Database(string? name)
    {
        return DbInstance(name).Database();
    }

    /// <summary>
    /// Gets the name of the database a given entity type is attached to. Returns name of default database if not specifically attached.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public static string DatabaseName<T>() where T : IEntity
        => Cache<T>.DbName;

    /// <summary>
    /// Switches the default database at runtime
    /// <para>WARNING: Use at your own risk!!! Might result in entities getting saved in the wrong databases under high concurrency situations.</para>
    /// <para>TIP: Make sure to cancel any watchers (change-streams) before switching the default database.</para>
    /// </summary>
    /// <param name="name">The name of the database to mark as the new default database</param>
    public static void ChangeDefaultDatabase(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name), "Database name cannot be null or empty");
        
        var dbInstance = DBInstance.Instance(name);

        _defaultDbInstance = dbInstance ?? throw new InvalidOperationException($"Database connection is not initialized for [{(string.IsNullOrEmpty(name) ? "Default" : name)}]");

        TypeMap.Clear();
        DefaultDbChanged?.Invoke();
    }

    /// <summary>
    /// Exposes the mongodb Filter Definition Builder for a given type.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public static FilterDefinitionBuilder<T> Filter<T>() where T : IEntity
        => Builders<T>.Filter;

    /// <summary>
    /// Exposes the mongodb Sort Definition Builder for a given type.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public static SortDefinitionBuilder<T> Sort<T>() where T : IEntity
        => Builders<T>.Sort;

    /// <summary>
    /// Exposes the mongodb Projection Definition Builder for a given type.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public static ProjectionDefinitionBuilder<T> Projection<T>() where T : IEntity
        => Builders<T>.Projection;

    /// <summary>
    /// Returns a new instance of the supplied IEntity type
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public static T Entity<T>() where T : IEntity, new()
        => new();

    /// <summary>
    /// Returns a new instance of the supplied IEntity type with the ID set to the supplied value
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="ID">The ID to set on the returned instance</param>
    public static T Entity<T>(object ID) where T : IEntity, new()
    {
        var newT = new T();
        newT.SetId(ID);

        return newT;
    }

    /// <summary>
    /// Initializes the ASP.NET Core Dependency Injection provider.
    /// Call this during application startup.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider" /> instance</param>
    public static void SetServiceProvider(IServiceProvider serviceProvider)
        => ServiceProvider = serviceProvider;
}