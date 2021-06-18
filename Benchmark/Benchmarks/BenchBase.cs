using MongoDB.Driver;
using MongoDB.Entities;
using System.Threading.Tasks;

namespace Benchmark
{
    public abstract class BenchBase
    {
        protected static IMongoDatabase Database { get; private set; }

        protected static IMongoCollection<Author> AuthorCollection { get; private set; }

        protected static IMongoCollection<Book> BookCollection { get; private set; }

        protected static void Initialize()
        {
            DB.InitAsync("mongodb-entities-benchmark").GetAwaiter().GetResult();
            Database = new MongoClient("mongodb://localhost").GetDatabase("mongodb-entities-benchmark");
            AuthorCollection = Database.GetCollection<Author>("Author");
            BookCollection = Database.GetCollection<Book>("Author");

            DB.DropCollectionAsync<Author>().GetAwaiter().GetResult();
            DB.DropCollectionAsync<Book>().GetAwaiter().GetResult();
        }

        public abstract Task MongoDB_Entities();

        public abstract Task Official_Driver();
    }
}
