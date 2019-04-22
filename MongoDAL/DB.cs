using System;
using System.Linq;
using MongoDB.Driver;
using Pluralize.NET;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization.Conventions;
using System.Threading.Tasks;

namespace MongoDAL
{
    public class DB
    {
        private static IMongoDatabase _db = null;
        private static Pluralizer _plural;

        /// <summary>
        /// Initializes the MongoDB connection with the given connection parameters.
        /// </summary>
        /// <param name="database">Name of the database</param>
        /// <param name="host">Adderss of the MongoDB server</param>
        /// <param name="port">Port number of the server</param>
        public DB(string database, string host = "127.0.0.1", int port = 27017)
        {

            Initialize(
                new MongoClientSettings { Server = new MongoServerAddress(host, port) },
                database);
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

        private void Initialize(MongoClientSettings settings, string database)
        {
            if (_db != null) throw new InvalidOperationException("Database connection is already initialized!");
            if (string.IsNullOrEmpty(database)) throw new ArgumentNullException("Database", "Database name cannot be empty!");

            try
            {
                _db = new MongoClient(settings).GetDatabase(database);
            }
            catch (Exception)
            {
                _db = null;
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

        private static IMongoCollection<T> Coll<T>()
        {
            return _db.GetCollection<T>(CollectionName<T>());
        }

        internal static IMongoCollection<Reference> Coll<TParent, TChild>()
        {
            return _db.GetCollection<Reference>(typeof(TParent).Name + "_" + typeof(TChild).Name);
        }

        /// <summary>
        /// Exposes MongoDB collections as Iqueryable in order to facilitate LINQ queries.
        /// </summary>
        /// <typeparam name="T">Any class that inherits from MongoEntity</typeparam>
        public static IMongoQueryable<T> Collection<T>()
        {
            CheckIfInitialized();
            return Coll<T>().AsQueryable();
        }

        /// <summary>
        /// Persists an entity to MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that inherits from MongoEntity</typeparam>
        /// <param name="entity">The instance to persist</param>
        public static void Save<T>(T entity) where T : Entity
        {
            SaveAsync<T>(entity).Wait();
        }

        /// <summary>
        /// Persists an entity to MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that inherits from MongoEntity</typeparam>
        /// <param name="entity">The instance to persist</param>
        public static Task SaveAsync<T>(T entity) where T : Entity
        {
            CheckIfInitialized();
            if (string.IsNullOrEmpty(entity.ID)) entity.ID = ObjectId.GenerateNewId().ToString();
            entity.ModifiedOn = DateTime.UtcNow;

            return Coll<T>().ReplaceOneAsync(
                x => x.ID.Equals(entity.ID),
                entity,
                new UpdateOptions() { IsUpsert = true });
        }

        /// <summary>
        /// Deletes a single entity from MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that inherits from MongoEntity</typeparam>
        /// <param name="id">The Id of the entity to delete</param>
        public static void Delete<T>(string id) where T : Entity
        {
            DeleteAsync<T>(id).Wait();
        }

        /// <summary>
        /// Deletes a single entity from MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that inherits from MongoEntity</typeparam>
        /// <param name="id">The Id of the entity to delete</param>
        public static Task DeleteAsync<T>(string id) where T : Entity
        {
            CheckIfInitialized();

            //todo: delete references from Parent_Child and Child_Parent collections.

            return Coll<T>().DeleteOneAsync(x => x.ID.Equals(id));
        }

        /// <summary>
        /// Delete multiple entities from MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that inherits from MongoEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        public static void Delete<T>(Expression<Func<T, bool>> expression) where T : Entity
        {
            DeleteAsync<T>(expression).Wait();
        }

        /// <summary>
        /// Delete multiple entities from MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that inherits from MongoEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        public static Task DeleteAsync<T>(Expression<Func<T, bool>> expression) where T : Entity
        {
            CheckIfInitialized();

            //todo: delete refernces from both side collections Product_Category and Category_Product

            return Coll<T>().DeleteManyAsync(expression);
        }

        private static void CheckIfInitialized()
        {
            if (_db == null)
            {
                throw new InvalidOperationException("Database connection is not initialized. Check Readme.md on how to initialize.");
            }
        }

    }
}
