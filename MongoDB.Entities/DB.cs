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
    public partial class DB
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

        //todo: remove obsoletes at version 15
        [Obsolete("Please use the .Database() method...")]
        public string DbName { get => dbName; }

        [Obsolete("Please use .DatabaseName<T>() method...")]
        public static string Database<T>() where T : IEntity => DatabaseName<T>();

        [Obsolete("Please use .DatabaseName() method...")]
        public string Database() => DatabaseName();

        internal string dbName;

        private static readonly Dictionary<string, IMongoDatabase> dbs = new Dictionary<string, IMongoDatabase>();
        private static readonly Dictionary<string, DB> instances = new Dictionary<string, DB>();

        /// <summary>
        /// Initializes the MongoDB connection with the given connection parameters.
        /// </summary>
        /// <param name="database">Name of the database</param>
        /// <param name="host">Adderss of the MongoDB server</param>
        /// <param name="port">Port number of the server</param>
        public DB(string database, string host = "127.0.0.1", int port = 27017)
        {
            Initialize(
                new MongoClientSettings { Server = new MongoServerAddress(host, port) }, database);
        }

        /// <summary>
        /// Initializes the MongoDB connection with an advanced set of parameters.
        /// </summary>
        /// <param name="settings">A MongoClientSettings object</param>
        /// <param name="database">Name of the database</param>
        public DB(MongoClientSettings settings, string database)
        {
            Initialize(settings, database);
        }

        private void Initialize(MongoClientSettings settings, string db)
        {
            if (string.IsNullOrEmpty(db)) throw new ArgumentNullException("database", "Database name cannot be empty!");

            dbName = db;

            if (dbs.ContainsKey(db)) return;

            try
            {
                dbs.Add(db, new MongoClient(settings).GetDatabase(db));
                instances.Add(db, this);
                dbs[db].ListCollectionNames().ToList(); //get the list of collection names so that first db connection is established
            }
            catch (Exception)
            {
                dbs.Remove(db);
                instances.Remove(db);
                dbName = null;
                throw;
            }
        }

        /// <summary>
        /// Gets the DB instance for a given database name.
        /// </summary>
        /// <param name="database">The database name to retrieve the DB instance for. Pass 'default' to retrieve the default instance</param>
        /// <exception cref="InvalidOperationException">Throws an exeception if the database has not yet been initialized</exception>
        public static DB GetInstance(string database)
        {
            if (database == default || database == null)
            {
                if (instances.Count > 0)
                    return instances.ElementAt(0).Value;
                throw new InvalidOperationException("No instances have been initialized yet!");
            }

            if (instances.ContainsKey(database)) return instances[database];

            throw new InvalidOperationException($"An instance has not been initialized yet for [{database}]");
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
        /// Gets the IMongoDatabase for a given database name.
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

            if (db == null) throw new InvalidOperationException($"Database connection is not initialized for [{name}]");

            return db;
        }

        /// <summary>
        /// Gets the IMongoDatabase of this instance
        /// </summary>
        public IMongoDatabase GetDatabase()
        {
            return GetDatabase(dbName);
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
        /// Returns the name of the database this instance was created with
        /// </summary>
        public string DatabaseName()
        {
            return dbName;
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

            var dbAttrb = type.GetCustomAttribute<DatabaseAttribute>(false);
            DBName = dbAttrb != null ? dbAttrb.Name : DB.GetInstance(default).dbName;

            Database = DB.GetDatabase(DBName);

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
