using System;
using System.Linq;
using MongoDB.Driver;
using Pluralize.NET;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization.Conventions;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;

namespace MongoDB.Entities
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

            ConventionRegistry.Register(
                "IgnoreManyProperties",
                new ConventionPack { new IgnoreManyPropertiesConvention() },
                type => true);

            _plural = new Pluralizer();
        }

        private static string CollectionName<T>()
        {
            return _plural.Pluralize(typeof(T).Name);
        }

        private static IMongoCollection<T> GetCollection<T>()
        {
            return _db.GetCollection<T>(CollectionName<T>());
        }

        internal static IMongoCollection<Reference> GetRefCollection(string name)
        {
            CheckIfInitialized();
            return _db.GetCollection<Reference>(name);
        }

        /// <summary>
        /// Exposes MongoDB collections as IQueryable in order to facilitate LINQ queries.
        /// </summary>
        /// <typeparam name="T">Any class that inherits from Entity</typeparam>
        public static IMongoQueryable<T> Collection<T>()
        {
            CheckIfInitialized();
            return GetCollection<T>().AsQueryable();
        }

        /// <summary>
        /// Persists an entity to MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that inherits from Entity</typeparam>
        /// <param name="entity">The instance to persist</param>
        public static void Save<T>(T entity) where T : Entity
        {
            SaveAsync<T>(entity).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Persists an entity to MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that inherits from Entity</typeparam>
        /// <param name="entity">The instance to persist</param>
        async public static Task SaveAsync<T>(T entity) where T : Entity
        {
            CheckIfInitialized();
            if (string.IsNullOrEmpty(entity.ID)) entity.ID = ObjectId.GenerateNewId().ToString();
            entity.ModifiedOn = DateTime.UtcNow;

            await GetCollection<T>()
                 .ReplaceOneAsync(x => x.ID.Equals(entity.ID),
                  entity,
                  new UpdateOptions() { IsUpsert = true });
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that inherits from Entity</typeparam>
        /// <param name="id">The Id of the entity to delete</param>
        public static void Delete<T>(string id) where T : Entity
        {
            DeleteAsync<T>(id).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that inherits from Entity</typeparam>
        /// <param name="id">The Id of the entity to delete</param>
        async public static Task DeleteAsync<T>(string id) where T : Entity
        {
            CheckIfInitialized();

            var collectionNames = await _db.ListCollectionNames().ToListAsync();

            //Book
            var typeName = typeof(T).Name;

            //[(PropName)Book~Author(PropName)] / [Book~Author(PropName)]
            var parentCollections = collectionNames.Where(name => name.Contains(typeName + "~")).ToArray();

            //[(PropName)Author~Book(PropName)] / [Author~Book(PropName)]
            var childCollections = collectionNames.Where(name => name.Contains("~" + typeName)).ToArray();

            var tasks = new List<Task>();

            foreach (var cName in parentCollections)
            {
                tasks.Add(_db.GetCollection<Reference>(cName).DeleteManyAsync(r => r.ParentID.Equals(id)));
            }

            foreach (var cName in childCollections)
            {
                tasks.Add(_db.GetCollection<Reference>(cName).DeleteManyAsync(r => r.ChildID.Equals(id)));
            }

            tasks.Add(GetCollection<T>().DeleteOneAsync(x => x.ID.Equals(id)));

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that inherits from Entity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        public static void Delete<T>(Expression<Func<T, bool>> expression) where T : Entity
        {
            DeleteAsync<T>(expression).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that inherits from Entity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        async public static Task DeleteAsync<T>(Expression<Func<T, bool>> expression) where T : Entity
        {
            var IDs = await DB.Collection<T>()
                              .Where(expression)
                              .Select(e => e.ID)
                              .ToListAsync();

            foreach (var id in IDs)
            {
                await DeleteAsync<T>(id);
            }
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that inherits from Entity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        public static void Delete<T>(IEnumerable<String> IDs) where T : Entity
        {
            DeleteAsync<T>(IDs).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that inherits from Entity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        async public static Task DeleteAsync<T>(IEnumerable<String> IDs) where T : Entity
        {
            foreach (var id in IDs)
            {
                await DeleteAsync<T>(id);
            }
        }

        //todo: find entity by ID + ID[] + lambda

        private static void CheckIfInitialized()
        {
            if (_db == null) throw new InvalidOperationException("Database connection is not initialized!");
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
