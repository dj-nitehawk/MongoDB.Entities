using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace MongoDB.Entities
{
    public partial class DB
    {
        /// <summary>
        /// Gets the database name of this instance
        /// </summary>
        public string DbName { get; private set; } = null;

        private static readonly Dictionary<string, IMongoDatabase> dbs = new Dictionary<string, IMongoDatabase>();
        private static readonly Dictionary<string, DB> instances = new Dictionary<string, DB>();
        private static bool isSetupDone = false;

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

            DbName = db;

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
                DbName = null;
                throw;
            }

            if (!isSetupDone)
            {
                BsonSerializer.RegisterSerializer(new DateSerializer());
                BsonSerializer.RegisterSerializer(new FuzzyStringSerializer());
                BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
                BsonSerializer.RegisterSerializer(typeof(decimal?), new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));

                ConventionRegistry.Register(
                    "IgnoreExtraElements",
                    new ConventionPack { new IgnoreExtraElementsConvention(true) },
                    type => true);

                ConventionRegistry.Register(
                    "IgnoreManyProperties",
                    new ConventionPack { new IgnoreManyPropertiesConvention() },
                    type => true);

                isSetupDone = true;
            }
        }

        private static IMongoDatabase GetDB(string database)
        {
            IMongoDatabase db = null;

            if (dbs.Count > 0)
            {
                if (string.IsNullOrEmpty(database))
                {
                    db = dbs.First().Value;
                }
                else
                {
                    dbs.TryGetValue(database, out db);
                }
            }

            if (db == null) throw new InvalidOperationException($"Database connection is not initialized for [{database}]");

            return db;
        }

        internal static string GetCollectionName<T>()
        {
            string collection = typeof(T).Name;

            var attribute = typeof(T).GetTypeInfo().GetCustomAttribute<NameAttribute>();
            if (attribute != null)
            {
                collection = attribute.Name;
            }

            if (string.IsNullOrWhiteSpace(collection) || collection.Contains("~")) throw new ArgumentException("This is an illegal name for a collection!");

            return collection;
        }

        internal static IMongoCollection<JoinRecord> GetRefCollection(string name, string db = null)
        {
            return GetDB(db).GetCollection<JoinRecord>(name);
        }

        internal static IMongoClient GetClient(string db = null)
        {
            return GetDB(db).Client;
        }

        /// <summary>
        /// Returns the DB instance for a given database name.
        /// </summary>
        /// <param name="database">The database name to retrieve the DB instance for. Setting null will retrieve the default instance</param>
        /// <exception cref="InvalidOperationException">Throws an exeception if the database has not yet been initialized</exception>
        public static DB GetInstance(string database)
        {
            if (database == null)
            {
                if (instances.Count > 0)
                    return instances.ElementAt(0).Value;
                throw new InvalidOperationException("No instances have been initialized yet!");
            }

            if (instances.ContainsKey(database)) return instances[database];

            throw new InvalidOperationException($"An instance has not been initialized yet for [{database}]");
        }

        /// <summary>
        /// Gets the IMongoCollection for a given IEntity type.
        /// <para>TIP: Try never to use this unless really neccessary.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static IMongoCollection<T> Collection<T>(string db = null)
        {
            return GetDB(db).GetCollection<T>(GetCollectionName<T>());
        }

        /// <summary>
        /// Gets the IMongoCollection for a given IEntity type.
        /// <para>TIP: Try never to use this unless really neccessary.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public IMongoCollection<T> Collection<T>()
        {
            return Collection<T>(DbName);
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
        /// Executes migration classes that implement the IMigration interface in the correct order to transform the database.
        /// <para>TIP: Write classes with names such as: _001_rename_a_field.cs, _002_delete_a_field.cs, etc. and implement IMigration interface on them. Call this method at the startup of the application in order to run the migrations.</para>
        /// </summary>
        public static void Migrate()
        {
            var excludes = new[]
            {
                "Microsoft.",
                "System.",
                "MongoDB.",
                "testhost.",
                "netstandard",
                "Newtonsoft.",
                "mscorlib",
                "NuGet."
            };

            var types = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a =>
                      (a.IsDynamic != true && !excludes.Any(n => a.FullName.StartsWith(n))) ||
                      (a.FullName.StartsWith("MongoDB.Entities.Tests")))
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetInterfaces().Contains(typeof(IMigration)));

            if (!types.Any())
                throw new InvalidOperationException("Didn't find any classes that implement IMigrate interface.");

            var lastMigration = Find<Migration>()
                    .Sort(m => m.Number, Order.Descending)
                    .Limit(1)
                    .Execute()
                    .SingleOrDefault();

            var lastMigNum = lastMigration != null ? lastMigration.Number : 0;

            var migrations = new SortedDictionary<int, IMigration>();

            foreach (var t in types)
            {
                var success = int.TryParse(t.Name.Split('_')[1], out int migNum);

                if (!success)
                    throw new InvalidOperationException("Failed to parse migration number from the class name. Make sure to name the migration classes like: _001_some_migration_name.cs");

                if (migNum > lastMigNum)
                    migrations.Add(migNum, (IMigration)Activator.CreateInstance(t));
            }

            var sw = new Stopwatch();

            foreach (var migration in migrations)
            {
                sw.Start();
                migration.Value.Upgrade();
                var mig = new Migration
                {
                    Number = migration.Key,
                    Name = migration.Value.GetType().Name,
                    TimeTakenSeconds = sw.Elapsed.TotalSeconds
                };
                Save(mig);
                sw.Stop();
                sw.Reset();
            }
        }

        /// <summary>
        /// Returns a new instance of the supplied IEntity type
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <returns></returns>
        public static T Entity<T>() where T : IEntity, new()
        {
            return new T();
        }
    }

    internal class IgnoreManyPropertiesConvention : ConventionBase, IMemberMapConvention
    {
        public void Apply(BsonMemberMap mMap)
        {
            if (mMap.MemberType.Name == "Many`1")
            {
                mMap.SetShouldSerializeMethod(o => false);
            }
        }
    }
}
