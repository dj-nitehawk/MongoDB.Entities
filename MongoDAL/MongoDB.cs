using System;
using System.Linq;
using MongoDB.Driver;
using Pluralize.NET;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using System.Linq.Expressions;

namespace MongoDAL
{
    public class DB
    {
        private static IMongoDatabase _db;
        private static Pluralizer _plural;

        public DB(string Database, string Host, string Port)
        {
            if (_db != null) throw new InvalidOperationException("Database connection is already initialized!");

            if (string.IsNullOrEmpty(Database)) throw new ArgumentNullException("Database", "Database name cannot be empty");

            _db = new MongoClient($"mongodb://{Host}:{Port}").GetDatabase(Database);
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

        public static IMongoQueryable<T> Collection<T>()
        {
            CheckIfInitialized();
            return collection<T>().AsQueryable();
        }

        public static void Save<T>(T entity) where T : MongoEntity
        {
            CheckIfInitialized();

            if (entity.Id.Equals(ObjectId.Empty)) entity.Id = ObjectId.GenerateNewId();

            collection<T>().ReplaceOne(
                x => x.Id.Equals(entity.Id),
                entity,
                new UpdateOptions() { IsUpsert = true });
        }

        public static void Delete<T>(ObjectId id) where T : MongoEntity
        {
            CheckIfInitialized();

            collection<T>().DeleteOne(x => x.Id.Equals(id));
        }

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
