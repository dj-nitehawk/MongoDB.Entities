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
public partial class DB
{
    static DB()
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

    /// <summary>
    /// The cached MongoClient instances
    /// </summary>
    static readonly ConcurrentDictionary<MongoClientSettings, MongoClient> _clients = new();
    
    static readonly ConcurrentDictionary<MongoClient, ConcurrentDictionary<string, DB>> _clientInstances = new();
    
    static MongoClientSettings _defaultClientSettings = null!; // to be set on first InitAsync call
    
    static DB _defaultInstance = null!; // to be set on first InitAsync call

    readonly IMongoDatabase _mongoDatabase;
    
    private DB(IMongoDatabase db)
    {
        _mongoDatabase = db;
    }

    /// <summary>
    /// Returns the cached DB instance or creates and initializes a new DB instance with the given connection parameters.
    /// <para>WARNING: will throw an error if server is not reachable!</para>
    /// You can call this method as many times as you want (such as in serverless functions) with the same parameters and the connections won't get
    /// duplicated.
    /// </summary>
    /// <param name="dbName">Name of the database</param>
    /// <param name="settings">A MongoClientSettings object</param>
    /// <param name="skipNetworkPing">Should we ping the database</param>
    /// <returns>DB instance</returns>
    public static async Task<DB> InitAsync(string dbName, MongoClientSettings? settings = null, bool skipNetworkPing = false)
    {
        if (settings == null)
        {
            if (_clients.Count == 0)
            {
                settings = new() { Server = new("127.0.0.1", 27017) };
            }
            else
            {
                settings = _defaultClientSettings;
            }
        }
        
        if (string.IsNullOrEmpty(dbName))
            throw new ArgumentNullException(nameof(dbName), "Database name cannot be empty!");
        
        if(!_clients.TryGetValue(settings, out var client))
        {
            client = new (settings);
            _clients.TryAdd(settings, client);
            _clientInstances.TryAdd(client, new ());

            if (_clients.Count==1)
                _defaultClientSettings = settings;
        }

        if (!_clientInstances.TryGetValue(client, out var instances))
        {
            throw new InvalidOperationException("clientInstances is not initialized");
        }
        
        if (!instances.TryGetValue(dbName, out var db))
        {
            try
            {
                var mongoDatabase = client.GetDatabase(dbName);
                db = new(mongoDatabase);
                
                if (settings==_defaultClientSettings && instances.Count==0)
                    _defaultInstance = db;

                if (instances.TryAdd(dbName, db) && !skipNetworkPing)
                    await mongoDatabase.RunCommandAsync((Command<BsonDocument>)"{ping:1}").ConfigureAwait(false);
            }
            catch (Exception)
            {
                instances.TryRemove(dbName, out _);
                throw;
            }
        }

        
        return db;
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
    /// Gets the DB instance for a given database name if it has been previously initialized.
    /// </summary>
    /// <param name="dbName">The name of the database to retrieve</param>
    /// <param name="settings">The host we want the instance from</param>
    public static DB Instance(string? dbName=null, MongoClientSettings? settings=null)
    {
        if (settings == null)
        {
            if (_clients.Count == 0)
                throw new InvalidOperationException("No DB instance has been initialized yet with the given settings. Please call DB.InitAsync() first.");
            settings = _defaultClientSettings;
        }
        
        if (!_clients.TryGetValue(settings, out var client))
        {
            throw new InvalidOperationException("No DB instance has been initialized yet with the given settings. Please call DB.InitAsync() first.");
        }
        
        if (!_clientInstances.TryGetValue(client, out var instances))
        {
            throw new InvalidOperationException("No DB instance has been initialized yet with the given settings. Please call DB.InitAsync() first.");
        }
        
        if (string.IsNullOrEmpty(dbName))
        {
            if (instances.Count == 0)
                throw new InvalidOperationException("No DB instance has been initialized yet. Please call DB.InitAsync() first.");

            return _defaultInstance;
        }
        
        instances.TryGetValue(dbName, out var db);

        return db ?? throw new InvalidOperationException($"No DB instance with the dbName '{dbName}' has been initialized yet. Please call DB.InitAsync() first.");
    }

    /// <summary>
    /// returns the default DB instance if null, otherwise db
    /// </summary>
    /// <param name="db">The DB instance to check</param>
    public static DB InstanceOrDefault(DB? db)
        => db ?? _defaultInstance;

    /// <summary>
    /// Gets the name of the database a given entity type is attached to. Returns name of default database if not specifically attached.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public string DatabaseName<T>() where T : IEntity
        => _mongoDatabase.DatabaseNamespace.DatabaseName;

    /// <summary>
    /// Switches the default database at runtime
    /// <para>WARNING: Use at your own risk!!! Might result in entities getting saved in the wrong databases under high concurrency situations.</para>
    /// <para>TIP: Make sure to cancel any watchers (change-streams) before switching the default database.</para>
    /// </summary>
    /// <param name="name">The name of the database to mark as the new default database</param>
    /// <param name="settings">The MongoClient we want to get the database from</param>
    public static void ChangeDefaultDatabase(string name, MongoClientSettings? settings = null)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name), "Database name cannot be null or empty");

        _defaultInstance = Instance(name, settings);
    }

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
    public T Entity<T>() where T : IEntity, new()
        => new();

    /// <summary>
    /// Returns a new instance of the supplied IEntity type with the ID set to the supplied value
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <param name="ID">The ID to set on the returned instance</param>
    public T Entity<T>(object ID) where T : IEntity, new()
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