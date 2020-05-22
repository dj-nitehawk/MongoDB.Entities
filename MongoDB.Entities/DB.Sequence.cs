using MongoDB.Entities.Core;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DB
    {
        //NOTE: transaction support will not be added due to unpredictability with concurrency.

        /// <summary>
        /// Returns an atomically generated sequential number for the given Entity type everytime the method is called
        /// </summary>
        /// <typeparam name="T">The type of entity to get the next sequential number for</typeparam>
        /// <param name="cancellation">And optional cancellation token</param>
        public Task<ulong> NextSequentialNumberAsync<T>(CancellationToken cancellation = default) where T : IEntity
        {
            return NextSequentialNumberAsync<T>(DbName, cancellation);
        }

        /// <summary>
        /// Returns an atomically generated sequential number for the given Entity type everytime the method is called
        /// </summary>
        /// <typeparam name="T">The type of entity to get the next sequential number for</typeparam>
        /// <param name="cancellation">And optional cancellation token</param>
        public static Task<ulong> NextSequentialNumberAsync<T>(string db = null, CancellationToken cancellation = default) where T : IEntity
        {
            return
                new UpdateAndGet<SequenceCounter, ulong>(db: db)
                    .Match(s => s.ID == GetCollectionName<T>())
                    .Modify(b => b.Inc(s => s.Count, 1ul))
                    .Option(o => o.IsUpsert = true)
                    .Project(s => s.Count)
                    .ExecuteAsync(cancellation);
        }

        /// <summary>
        /// Returns an atomically generated sequential number for the given Entity type everytime the method is called
        /// </summary>
        /// <typeparam name="T">The type of entity to get the next sequential number for</typeparam>
        public ulong NextSequentialNumber<T>() where T : IEntity
        {
            return Run.Sync(() => NextSequentialNumberAsync<T>(DbName));
        }

        /// <summary>
        /// Returns an atomically generated sequential number for the given Entity type everytime the method is called
        /// </summary>
        /// <typeparam name="T">The type of entity to get the next sequential number for</typeparam>
        public static ulong NextSequentialNumber<T>(string db = null) where T : IEntity
        {
            return Run.Sync(() => NextSequentialNumberAsync<T>(db));
        }
    }
}
