using System;
using System.Linq;
using MongoDB.Driver;
using Pluralize.NET;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization.Conventions;

namespace MongoDAL
{
    public class DB
    {
        private static IMongoDatabase _db;
        private static Pluralizer _plural;

        internal DB(string database, string host, int port)
        {

            Initialize(
                new MongoClientSettings { Server = new MongoServerAddress(host, port) },
                database);
        }

        internal DB(MongoClientSettings settings, string database)
        {
            Initialize(settings, database);
        }

        private void Initialize(MongoClientSettings settings, string database)
        {
            if (_db != null) throw new InvalidOperationException("Database connection is already initialized!");
            if (string.IsNullOrEmpty(database)) throw new ArgumentNullException("Database", "Database name cannot be empty");

            try
            {
                _db = new MongoClient(settings).GetDatabase(database);
            }
            catch (Exception)
            {

                throw;
            }

            ConventionRegistry.Register(
                "IgnoreExtraElements",
                new ConventionPack { new IgnoreExtraElementsConvention(true) },
                type => true);

            _plural = new Pluralizer();
        }

        private static string CollectionName<T>()
        {
            return _plural.Pluralize(typeof(T).Name);
        }

        private static IMongoCollection<T> collection<T>()
        {
            return _db.GetCollection<T>(CollectionName<T>());
        }

        /// <summary>
        /// Exposes MongoDB collections as Iqueryable in order to facilitate LINQ queries.
        /// </summary>
        /// <typeparam name="T">Any class that inherits from MongoEntity</typeparam>
        public static IMongoQueryable<T> Collection<T>()
        {
            CheckIfInitialized();
            return collection<T>().AsQueryable();
        }

        /// <summary>
        /// Persists an entity to MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that inherits from MongoEntity</typeparam>
        /// <param name="entity">The instance to persist</param>
        public static void Save<T>(T entity) where T : MongoEntity
        {
            CheckIfInitialized();

            if (string.IsNullOrEmpty(entity.Id)) entity.Id = ObjectId.GenerateNewId().ToString();

            entity.ModifiedOn = DateTime.UtcNow;

            collection<T>().ReplaceOne(
                x => x.Id.Equals(entity.Id),
                entity,
                new UpdateOptions() { IsUpsert = true });
        }

        /// <summary>
        /// Deletes a single entity from MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that inherits from MongoEntity</typeparam>
        /// <param name="id">The Id of the entity to delete</param>
        public static void Delete<T>(string id) where T : MongoEntity
        {
            CheckIfInitialized();

            collection<T>().DeleteOne(x => x.Id.Equals(id));
        }

        /// <summary>
        /// Delete multiple entities from MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that inherits from MongoEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        public static void DeleteMany<T>(Expression<Func<T, bool>> expression) where T : MongoEntity
        {
            CheckIfInitialized();

            collection<T>().DeleteMany(expression);
        }

        private static void CheckIfInitialized()
        {
            if (_db == null)
            {
                throw new InvalidOperationException("Database connection is not initialized. Add 'services.AddMongoDAL()' in Startup.cs");
            }
        }
    }
}
