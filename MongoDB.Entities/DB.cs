using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        static DB()
        {
            BsonSerializer.RegisterSerializer(new DateSerializer());
            BsonSerializer.RegisterSerializer(new FuzzyStringSerializer());
            BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
            BsonSerializer.RegisterSerializer(typeof(decimal?), new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));

            ConventionRegistry.Register(
                "DefaultConvetions",
                new ConventionPack
                {
                        new IgnoreExtraElementsConvention(true),
                        new IgnoreManyPropertiesConvention()
                },
                _ => true);
        }

        private static readonly Dictionary<string, IMongoDatabase> dbs = new Dictionary<string, IMongoDatabase>();

        /// <summary>
        /// Initializes a MongoDB connection with the given connection parameters.
        /// You can call this method as many times as you want (such as in serveless functions) with the same parameters and the connections won't get duplicated.
        /// </summary>
        /// <param name="database">Name of the database</param>
        /// <param name="host">Adderss of the MongoDB server</param>
        /// <param name="port">Port number of the server</param>
        public static Task InitAsync(string database, string host = "127.0.0.1", int port = 27017)
        {
            return Initialize(
                new MongoClientSettings { Server = new MongoServerAddress(host, port) }, database);
        }

        /// <summary>
        /// Initializes a MongoDB connection with the given connection parameters.
        /// You can call this method as many times as you want (such as in serveless functions) with the same parameters and the connections won't get duplicated.
        /// </summary>
        /// <param name="database">Name of the database</param>
        /// <param name="settings">A MongoClientSettings object</param>
        public static Task InitAsync(string database, MongoClientSettings settings)
        {
            return Initialize(settings, database);
        }

        private static async Task Initialize(MongoClientSettings settings, string dbName)
        {
            if (string.IsNullOrEmpty(dbName)) throw new ArgumentNullException("database", "Database name cannot be empty!");

            if (dbs.ContainsKey(dbName)) return;

            try
            {
                dbs.Add(dbName, new MongoClient(settings).GetDatabase(dbName));
                await dbs[dbName].ListCollectionNamesAsync().ConfigureAwait(false); //get a cursor for the list of collection names so that first db connection is established
            }
            catch (Exception)
            {
                dbs.Remove(dbName);
                throw;
            }
        }

        /// <summary>
        /// Gets the IMongoDatabase for the given entity type
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        public static IMongoDatabase Database<T>() where T : IEntity
        {
            return Cache<T>.Database;
        }

        /// <summary>
        /// Gets the IMongoDatabase for a given database name if it has been previously initialized.
        /// You can also get the default database by passing 'default' or 'null' for the name parameter.
        /// </summary>
        /// <param name="name">The name of the database to retrieve</param>
        public static IMongoDatabase Database(string name)
        {
            IMongoDatabase db = null;

            if (dbs.Count > 0)
            {
                if (string.IsNullOrEmpty(name))
                {
                    db = dbs.First().Value;
                }
                else
                {
                    dbs.TryGetValue(name, out db);
                }
            }

            if (db == null)
                throw new InvalidOperationException($"Database connection is not initialized for [{(string.IsNullOrEmpty(name) ? "Default" : name)}]");

            return db;
        }

        /// <summary>
        /// Gets the name of the database a given entity type is attached to. Returns name of default database if not specifically attached.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static string DatabaseName<T>() where T : IEntity
        {
            return Cache<T>.DBName;
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
        /// Returns a new ObjectId as a string
        /// </summary>
        public static string NewID() => ObjectId.GenerateNewId().ToString();
    }

    internal static class TypeMap
    {
        private static readonly Dictionary<Type, IMongoDatabase> TypeToDBMap = new Dictionary<Type, IMongoDatabase>();
        private static readonly Dictionary<Type, string> TypeToCollMap = new Dictionary<Type, string>();

        internal static void AddCollectionMapping(Type entityType, string collectionName)
            => TypeToCollMap.Add(entityType, collectionName);

        internal static string GetCollectionName(Type entityType)
            => TypeToCollMap[entityType];

        internal static void AddDatabaseMapping(Type entityType, IMongoDatabase database)
            => TypeToDBMap.Add(entityType, database);

        internal static IMongoDatabase GetDatabase(Type entityType)
            => TypeToDBMap[entityType];
    }

    internal static class Cache<T> where T : IEntity
    {
        internal static IMongoDatabase Database { get; }
        internal static IMongoCollection<T> Collection { get; }
        internal static string DBName { get; }
        internal static string CollectionName { get; }
        internal static Dictionary<string, Watcher<T>> Watchers { get; }
        internal static bool HasCreatedOn { get; }
        internal static bool HasModifiedOn { get; }
        internal static string ModifiedOnPropName { get; }

        static Cache()
        {
            var type = typeof(T);

            Database = DB.Database(type.GetCustomAttribute<DatabaseAttribute>(false)?.Name);
            DBName = Database.DatabaseNamespace.DatabaseName;
            TypeMap.AddDatabaseMapping(type, Database);

            var collAttrb = type.GetCustomAttribute<NameAttribute>(false);
            CollectionName = collAttrb != null ? collAttrb.Name : type.Name;

            if (string.IsNullOrWhiteSpace(CollectionName) || CollectionName.Contains("~"))
                throw new ArgumentException($"{CollectionName} is an illegal name for a collection!");

            Collection = Database.GetCollection<T>(CollectionName);
            TypeMap.AddCollectionMapping(type, CollectionName);

            Watchers = new Dictionary<string, Watcher<T>>();

            var interfaces = type.GetInterfaces();
            HasCreatedOn = interfaces.Any(it => it == typeof(ICreatedOn));
            HasModifiedOn = interfaces.Any(it => it == typeof(IModifiedOn));
            ModifiedOnPropName = nameof(IModifiedOn.ModifiedOn);
        }
    }

    internal class IgnoreManyPropertiesConvention : ConventionBase, IMemberMapConvention
    {
        public void Apply(BsonMemberMap mMap)
        {
            if (mMap.MemberType.Name == ManyBase.PropType)
            {
                _ = mMap.SetShouldSerializeMethod(_ => false);
            }
        }
    }
}

//todo: update readme with new async code before releasing v20.0

//todo: merge v20 wiki repo to master after releasing v20.0
