using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
public partial class DBInstance
{
    static DBInstance()
    {
        BsonSerializer.RegisterSerializer(new DateSerializer());
        BsonSerializer.RegisterSerializer(new FuzzyStringSerializer());
        BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
        BsonSerializer.RegisterSerializer(typeof(decimal?), new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));

        ConventionRegistry.Register(
            "DefaultConventions",
            new ConventionPack
            {
                new IgnoreExtraElementsConvention(true),
                new IgnoreManyPropsConvention()
            },
            _ => true);
    }

    internal IServiceProvider? ServiceProvider { get; private set; }

    static readonly ConcurrentDictionary<string, DBInstance> _instances = new();
    IMongoDatabase _mongoDatabase;
    
    private DBInstance(IMongoDatabase db)
    {
        _mongoDatabase = db;
    }

    /// <summary>
    /// Returns the cached DBInstance or creates and initializes a new DBInstance with the given connection parameters.
    /// <para>WARNING: will throw an error if server is not reachable!</para>
    /// You can call this method as many times as you want (such as in serverless functions) with the same parameters and the connections won't get
    /// duplicated.
    /// </summary>
    /// <param name="database">Name of the database</param>
    /// <param name="host">Address of the MongoDB server</param>
    /// <param name="port">Port number of the server</param>
    /// <returns>DBInstance</returns>
    public static Task<DBInstance> InitAsync(string database, string host = "127.0.0.1", int port = 27017)
        => Initialize(new() { Server = new(host, port) }, database);

    /// <summary>
    /// Returns the cached DBInstance or creates and initializes a new DBInstance with the given connection parameters.
    /// <para>WARNING: will throw an error if server is not reachable!</para>
    /// You can call this method as many times as you want (such as in serverless functions) with the same parameters and the connections won't get
    /// duplicated.
    /// </summary>
    /// <param name="database">Name of the database</param>
    /// <param name="settings">A MongoClientSettings object</param>
    /// <returns>DBInstance</returns>
    public static Task<DBInstance> InitAsync(string database, MongoClientSettings settings)
                                 => Initialize(settings, database);

    internal static async Task<DBInstance> Initialize(MongoClientSettings settings, string dbName, bool skipNetworkPing = false)
    {
        if (string.IsNullOrEmpty(dbName))
            throw new ArgumentNullException(nameof(dbName), "Database name cannot be empty!");

        if (!_instances.TryGetValue(dbName, out var dbInstance))
        {
            try
            {
                var db = new MongoClient(settings).GetDatabase(dbName);
                dbInstance = new(db);

                if (_instances.TryAdd(dbName, dbInstance) && !skipNetworkPing)
                    await db.RunCommandAsync((Command<BsonDocument>)"{ping:1}").ConfigureAwait(false);
            }
            catch (Exception)
            {
                _instances.TryRemove(dbName, out _);

                throw;
            }
        }
        
        return dbInstance;
    }

    /// <summary>
    /// Gets a list of all database names from the server
    /// </summary>
    /// <param name="host">Address of the MongoDB server</param>
    /// <param name="port">Port number of the server</param>
    public Task<IEnumerable<string>> AllDatabaseNamesAsync(string host = "127.0.0.1", int port = 27017)
        => AllDatabaseNamesAsync(new() { Server = new(host, port) });

    /// <summary>
    /// Gets a list of all database names from the server
    /// </summary>
    /// <param name="settings">A MongoClientSettings object</param>
    public async Task<IEnumerable<string>> AllDatabaseNamesAsync(MongoClientSettings settings)
        => await (await new MongoClient(settings).ListDatabaseNamesAsync().ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);

    /// <summary>
    /// Gets the IMongoDatabase for the given entity type
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    public IMongoDatabase Database<T>() where T : IEntity
        => _mongoDatabase;

    /// <summary>
    /// Gets the IMongoDatabase for a given database name if it has been previously initialized.
    /// You can also get the default database by passing 'default' or 'null' for the name parameter.
    /// </summary>
    public IMongoDatabase Database()
    {
        return _mongoDatabase;
    }

    /// <summary>
    /// Gets the DBInstance for a given database name if it has been previously initialized.
    /// </summary>
    /// <param name="name">The name of the database to retrieve</param>
    public static DBInstance? Instance(string? name)
    {
        DBInstance? dbInstance = null;

        if (!string.IsNullOrEmpty(name))
            _instances.TryGetValue(name, out dbInstance);

        return dbInstance;
    }

    /// <summary>
    /// Gets the name of the database a given entity type is attached to. Returns name of default database if not specifically attached.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public string DatabaseName<T>() where T : IEntity
        => _mongoDatabase.DatabaseNamespace.DatabaseName;

    /// <summary>
    /// Exposes the mongodb Filter Definition Builder for a given type.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public FilterDefinitionBuilder<T> Filter<T>() where T : IEntity
        => Builders<T>.Filter;

    /// <summary>
    /// Exposes the mongodb Sort Definition Builder for a given type.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public SortDefinitionBuilder<T> Sort<T>() where T : IEntity
        => Builders<T>.Sort;

    /// <summary>
    /// Exposes the mongodb Projection Definition Builder for a given type.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public ProjectionDefinitionBuilder<T> Projection<T>() where T : IEntity
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
    public void SetServiceProvider(IServiceProvider serviceProvider)
        => ServiceProvider = serviceProvider;
}