using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Benchmark.Benchmarks
{
    [MemoryDiagnoser]
    public class CreateOne : BenchBase
    {
        public CreateOne()
        {
            Initialize();
        }

        [Benchmark]
        public override Task MongoDB_Entities()
        {
            return DB.SaveAsync(new Author
            {
                FirstName = "test",
                LastName = "test",
                Birthday = DateTime.UtcNow,
            });
        }

        [Benchmark(Baseline = true)]
        public override Task Official_Driver()
        {
            return AuthorCollection.InsertOneAsync(new Author
            {
                ID = ObjectId.GenerateNewId().ToString(),
                FirstName = "test",
                LastName = "test",
                Birthday = DateTime.UtcNow,
            });
        }
    }

    [MemoryDiagnoser]
    public class CreateBulk : BenchBase
    {
        private readonly List<Author> list = new(1000);

        public CreateBulk()
        {
            Initialize();
            for (int i = 1; i <= 1000; i++)
            {
                list.Add(new Author
                {
                    FirstName = "test",
                    LastName = "test",
                    Birthday = DateTime.UtcNow
                });
            }
        }

        [Benchmark]
        public override Task MongoDB_Entities()
        {
            foreach (var author in list) author.ID = null;
            return DB.SaveAsync(list);
        }

        [Benchmark(Baseline = true)]
        public override Task Official_Driver()
        {
            var models = new List<WriteModel<Author>>(list.Count);
            foreach (var author in list)
            {
                author.ID = author.GenerateNewID();
                models.Add(new InsertOneModel<Author>(author));
            }
            return AuthorCollection.BulkWriteAsync(models);
        }
    }

    [MemoryDiagnoser]
    public class FindOne : BenchBase
    {
        private readonly List<Author> list = new(1000);
        private readonly string guid = Guid.NewGuid().ToString();

        public FindOne()
        {
            Initialize();

            DB.Index<Author>()
              .Key(a => a.FirstName, KeyType.Ascending)
              .Option(o => o.Background = false)
              .CreateAsync()
              .GetAwaiter()
              .GetResult();

            for (int i = 1; i <= 1000; i++)
            {
                list.Add(new Author
                {
                    FirstName = i == 500 ? guid : "test",
                });
            }
            list.SaveAsync().GetAwaiter().GetResult();
        }

        [Benchmark]
        public override Task MongoDB_Entities()
        {
            return DB
                .Find<Author>()
                .Match(x => x.FirstName == guid)
                .ExecuteAsync();
        }

        [Benchmark(Baseline = true)]
        public override async Task Official_Driver()
        {
            var filter = Builders<Author>.Filter.Where(a => a.FirstName == guid);
            await (await AuthorCollection.FindAsync(filter)).ToListAsync();
        }
    }

    [MemoryDiagnoser]
    public class UpdateOne : BenchBase
    {
        private readonly string id = ObjectId.GenerateNewId().ToString();

        public UpdateOne()
        {
            Initialize();
            DB.SaveAsync(new Author { ID = id, FirstName = "initial" }).GetAwaiter().GetResult();
        }

        [Benchmark]
        public override Task MongoDB_Entities()
        {
            return DB.Update<Author>()
                     .MatchID(id)
                     .Modify(a => a.FirstName, "updated")
                     .ExecuteAsync();
        }

        [Benchmark(Baseline = true)]
        public override Task Official_Driver()
        {
            var filter = Builders<Author>.Filter.Where(a => a.ID == id);
            var update = Builders<Author>.Update.Set(a => a.FirstName, "updated");
            return AuthorCollection.UpdateOneAsync(filter, update);
        }
    }
}
