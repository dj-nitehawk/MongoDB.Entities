using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
        public static void Init(string database, string host = "127.0.0.1", int port = 27017)
        {
            Initialize(
                new MongoClientSettings { Server = new MongoServerAddress(host, port) }, database);
        }

        /// <summary>
        /// Initializes a MongoDB connection with the given connection parameters.
        /// You can call this method as many times as you want (such as in serveless functions) with the same parameters and the connections won't get duplicated.
        /// </summary>
        /// <param name="database">Name of the database</param>
        /// <param name="settings">A MongoClientSettings object</param>
        public static void Init(string database, MongoClientSettings settings)
        {
            Initialize(settings, database);
        }

        private static void Initialize(MongoClientSettings settings, string db)
        {
            if (string.IsNullOrEmpty(db)) throw new ArgumentNullException("database", "Database name cannot be empty!");

            if (dbs.ContainsKey(db)) return;

            try
            {
                dbs.Add(db, new MongoClient(settings).GetDatabase(db));
                dbs[db].ListCollectionNames().ToList(); //get the list of collection names so that first db connection is established
            }
            catch (Exception)
            {
                dbs.Remove(db);
                throw;
            }
        }

        //todo: move GetDatabase<T>() to Database() after obsoletes are gone at v15

        /// <summary>
        /// Gets the IMongoDatabase for the given entity type
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        public static IMongoDatabase GetDatabase<T>() where T : IEntity
        {
            return Cache<T>.Database;
        }

        /// <summary>
        /// Gets the IMongoDatabase for a given database name if it has been previously initialized.
        /// You can also get the default database by passing 'default' or 'null' for the name parameter.
        /// </summary>
        /// <param name="name">The name of the database to retrieve</param>
        public static IMongoDatabase GetDatabase(string name)
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

            var dbName = string.IsNullOrEmpty(name) ? "Default" : name;

            if (db == null)
                throw new InvalidOperationException($"Database connection is not initialized for [{dbName}]");

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
    }

    internal static class Cache<T> where T : IEntity
    {
        public static IMongoDatabase Database { get; }
        public static IMongoCollection<T> Collection { get; }
        public static string DBName { get; }
        public static string CollectionName { get; }
        public static Dictionary<string, Watcher<T>> Watchers { get; set; }
        public static bool HasCreatedOn { get; set; }
        public static bool HasModifiedOn { get; set; }
        public static string ModifiedOnPropName { get; set; }

        static Cache()
        {
            var type = typeof(T);

            Database = DB.GetDatabase(
                type.GetCustomAttribute<DatabaseAttribute>(false)?.Name);

            DBName = Database.DatabaseNamespace.DatabaseName;

            var collAttrb = type.GetCustomAttribute<NameAttribute>(false);
            CollectionName = collAttrb != null ? collAttrb.Name : type.Name;

            if (string.IsNullOrWhiteSpace(CollectionName) || CollectionName.Contains("~"))
                throw new ArgumentException($"{CollectionName} is an illegal name for a collection!");

            Collection = Database.GetCollection<T>(CollectionName);

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
