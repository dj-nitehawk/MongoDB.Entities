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
        public static ulong NextSequentialNumber<T>() where T : IEntity
        {
            return NextSequentialNumber(CollectionName<T>());
        }

        /// <summary>
        /// Returns an atomically generated sequential number for the given Entity type everytime the method is called
        /// </summary>
        /// <typeparam name="T">The type of entity to get the next sequential number for</typeparam>
        public ulong NextSequentialNumber<T>(bool _ = false) where T : IEntity
        {
            return NextSequentialNumber<T>();
        }

        /// <summary>
        /// Returns an atomically generated sequential number for the given Entity type everytime the method is called
        /// </summary>
        /// <typeparam name="T">The type of entity to get the next sequential number for</typeparam>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<ulong> NextSequentialNumberAsync<T>(CancellationToken cancellation = default) where T : IEntity
        {
            return NextSequentialNumberAsync(CollectionName<T>(), cancellation);
        }

        /// <summary>
        /// Returns an atomically generated sequential number for the given Entity type everytime the method is called
        /// </summary>
        /// <typeparam name="T">The type of entity to get the next sequential number for</typeparam>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<ulong> NextSequentialNumberAsync<T>(CancellationToken cancellation = default, bool _ = false) where T : IEntity
        {
            return NextSequentialNumberAsync<T>(cancellation);
        }

        /// <summary>
        /// Returns an atomically generated sequential number for the given sequence name everytime the method is called
        /// </summary>
        /// <param name="sequenceName">The name of the sequence to get the next number for</param>
        public static ulong NextSequentialNumber(string sequenceName)
        {
            return UpdateAndGetCommand(sequenceName).Execute();
        }

        /// <summary>
        /// Returns an atomically generated sequential number for the given sequence name everytime the method is called
        /// </summary>
        /// <param name="sequenceName">The name of the sequence to get the next number for</param>
        public ulong NextSequentialNumber(string sequenceName, bool _ = false)
        {
            return NextSequentialNumber(sequenceName);
        }

        /// <summary>
        /// Returns an atomically generated sequential number for the given sequence name everytime the method is called
        /// </summary>
        /// <param name="sequenceName">The name of the sequence to get the next number for</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<ulong> NextSequentialNumberAsync(string sequenceName, CancellationToken cancellation = default)
        {
            return UpdateAndGetCommand(sequenceName).ExecuteAsync(cancellation);
        }

        /// <summary>
        /// Returns an atomically generated sequential number for the given sequence name everytime the method is called
        /// </summary>
        /// <param name="sequenceName">The name of the sequence to get the next number for</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<ulong> NextSequentialNumberAsync(string sequenceName, CancellationToken cancellation = default, bool _ = false)
        {
            return NextSequentialNumberAsync(sequenceName, cancellation);
        }

        private static UpdateAndGet<SequenceCounter, ulong> UpdateAndGetCommand(string sequenceName)
        {
            return new UpdateAndGet<SequenceCounter, ulong>()
                .Match(s => s.ID == sequenceName)
                .Modify(b => b.Inc(s => s.Count, 1ul))
                .Option(o => o.IsUpsert = true)
                .Project(s => s.Count);
        }
    }
}
