using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// The main entrypoint for all data access methods of the library
    /// </summary>
    /// <remarks>Please consider using DBContext directly, this will be made obsolete in the future</remarks>
    public static partial class DB
    {
        private static DBContext? _context;

        static DB()
        {
            DBContext.InitStatic();
        }

        /// <summary>
        /// Checks if InitAsync was called successfully
        /// </summary>
        public static bool IsInitialized => _context is not null;

        /// <summary>
        /// Contains the latest initialized context
        /// </summary>
        /// <exception cref="ArgumentException">Throws an exception if InitAsync wasn't called, check <see cref="IsInitialized"/></exception>
        public static DBContext Context
        {
            get => _context ?? throw new ArgumentException("The database hasn't been initialized yet! please call [InitAsync] first");
            set => _context = value;
        }

        /// <summary>
        /// Initializes a MongoDB connection with the given connection parameters.
        /// <para>WARNING: will throw an error if server is not reachable!</para>
        /// You can call this method as many times as you want (such as in serverless functions) with the same parameters and the connections won't get duplicated.
        /// </summary>
        /// <param name="database">Name of the database</param>
        /// <param name="host">Address of the MongoDB server</param>
        /// <param name="port">Port number of the server</param>
        /// <param name="skipNetworkPing">Whether to skip pinging the host on initialization, in which case the task completes instantly</param>
        public static Task InitAsync(string database, string host = "127.0.0.1", int port = 27017, bool skipNetworkPing = false)
        {
            return Initialize(
                new MongoClientSettings { Server = new MongoServerAddress(host, port) }, database, skipNetworkPing);
        }

        /// <summary>
        /// Initializes a MongoDB connection with the given connection parameters.
        /// <para>WARNING: will throw an error if server is not reachable!</para>
        /// You can call this method as many times as you want (such as in serverless functions) with the same parameters and the connections won't get duplicated.
        /// </summary>
        /// <param name="database">Name of the database</param>
        /// <param name="settings">A MongoClientSettings object</param>
        /// <param name="skipNetworkPing">Whether to skip pinging the host on initialization, in which case the task completes instantly</param>        
        public static Task InitAsync(string database, MongoClientSettings settings, bool skipNetworkPing = false)
        {
            return Initialize(settings, database, skipNetworkPing);
        }

        /// <summary>
        /// Initializes a MongoDB connection with the given DBContext parameters.
        /// <para>WARNING: will throw an error if server is not reachable!</para>
        /// </summary>
        /// <param name="context">The database context</param>
        /// <param name="skipNetworkPing">Whether to skip pinging the host on initialization, in which case the task completes instantly</param>
        public static async Task InitAsync(DBContext context, bool skipNetworkPing = false)
        {
            if (skipNetworkPing || await context.PingNetwork())
            {
                _context = context;
            }
            else
            {
                _context = null;
            }
        }


        internal static async Task Initialize(MongoClientSettings settings, string dbName, bool skipNetworkPing = false)
        {
            if (string.IsNullOrEmpty(dbName))
                throw new ArgumentNullException(nameof(dbName), "Database name cannot be empty!");

            if (dbName == _context?.DatabaseNamespace.DatabaseName) return;

            var newCtx = new DBContext(dbName, settings);
            if (skipNetworkPing || await newCtx.PingNetwork())
            {
                _context = newCtx;
            }
            else
            {
                _context = null;
            }
        }

        /// <summary>
        /// Gets a list of all database names from the server
        /// </summary>
        /// <param name="host">Address of the MongoDB server</param>
        /// <param name="port">Port number of the server</param>
        public static Task<IEnumerable<string>> AllDatabaseNamesAsync(string host = "127.0.0.1", int port = 27017)
        {
            return AllDatabaseNamesAsync(new MongoClientSettings { Server = new MongoServerAddress(host, port) });
        }

        /// <summary>
        /// Gets a list of all database names from the server
        /// </summary>
        /// <param name="settings">A MongoClientSettings object</param>
        public static async Task<IEnumerable<string>> AllDatabaseNamesAsync(MongoClientSettings settings)
        {
            return await (await
                new MongoClient(settings)
                .ListDatabaseNamesAsync().ConfigureAwait(false))
                .ToListAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Specifies the database that a given entity type should be stored in. 
        /// Only needed for entity types you want stored in a db other than the default db.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="databaseName">The name of the database</param>
        [Obsolete("This method does nothing", error: true)]
        public static void DatabaseFor<T>(string databaseName) where T : IEntity
        {
        }

        /// <summary>
        /// Gets the IMongoDatabase for the given entity type
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        [Obsolete("This method returns the current Context", error: true)]
        public static IMongoDatabase Database<T>() where T : IEntity
        {
            return Context;
        }

        /// <summary>
        /// Gets the IMongoDatabase for a given database name if it has been previously initialized.
        /// You can also get the default database by passing 'default' or 'null' for the name parameter.
        /// </summary>
        /// <param name="name">The name of the database to retrieve</param>
        public static IMongoDatabase Database(string? name)
        {
            return name == null ? Context : Context.MongoServerContext.GetDatabase(name);
        }

        /// <summary>
        /// Gets the name of the database a given entity type is attached to. Returns name of default database if not specifically attached.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        [Obsolete("This method returns the current DatabaseName in the Context")]
        public static string DatabaseName<T>() where T : IEntity
        {
            return Context.DatabaseNamespace.DatabaseName;
        }

        /// <summary>
        /// Exposes the mongodb Filter Definition Builder for a given type.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static FilterDefinitionBuilder<T> Filter<T>() where T : IEntity
        {
            return Builders<T>.Filter;
        }

        /// <summary>
        /// Exposes the mongodb Sort Definition Builder for a given type.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static SortDefinitionBuilder<T> Sort<T>() where T : IEntity
        {
            return Builders<T>.Sort;
        }

        /// <summary>
        /// Exposes the mongodb Projection Definition Builder for a given type.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static ProjectionDefinitionBuilder<T> Projection<T>() where T : IEntity
        {
            return Builders<T>.Projection;
        }

        /// <summary>
        /// Returns a new instance of the supplied IEntity type
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static T Entity<T>() where T : IEntity, new()
        {
            return new T();
        }

        /// <summary>
        /// Returns a new instance of the supplied IEntity type with the ID set to the supplied value
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="ID">The ID to set on the returned instance</param>
        public static T Entity<T>(string ID) where T : IEntity, new()
        {
            return new T { ID = ID };
        }
    }
}