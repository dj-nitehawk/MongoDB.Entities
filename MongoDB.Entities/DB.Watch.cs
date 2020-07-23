using MongoDB.Entities.Core;
using System;
using System.Linq.Expressions;
using System.Threading;

namespace MongoDB.Entities
{
    public partial class DB
    {
        /// <summary>
        /// Instantiates a watcher thread that will open up a mongodb change stream. 
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="eventTypes">Type of event to watch for. Specify more than one like: EventType.Created | EventType.Updated | EventType.Deleted</param>
        /// <param name="filter">x => x.Status == "completed"</param>
        /// <param name="batchSize">The max number of entities to receive for a single event occurence</param>
        /// <param name="cancellation">A cancellation token for ending the watch/ change stream</param>
        public static Watcher<T> Watch<T>(
            EventType eventTypes,
            Expression<Func<T, bool>> filter = null,
            int batchSize = 100,
            CancellationToken cancellation = default
            ) where T : IEntity

            => new Watcher<T>(eventTypes, filter, batchSize, cancellation);

        /// <summary>
        /// Instantiates a watcher thread that will open up a mongodb change stream. 
        /// You can project the result to a different form with this method.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <typeparam name="TResult">The projected type of the result</typeparam>
        /// <param name="eventTypes">Type of event to watch for. Specify more than one like: EventType.Created | EventType.Updated | EventType.Deleted</param>
        /// <param name="filter">x => x.Status == "completed"</param>
        /// <param name="projection">x => new { x.ID, x.SomeProp }</param>
        /// <param name="batchSize">The max number of entities to receive for a single event occurence</param>
        /// <param name="cancellation">A cancellation token for ending the watch/ change stream</param>
        public static Watcher<T, TResult> Watch<T, TResult>(
            EventType eventTypes,
            Expression<Func<T, TResult>> projection,
            Expression<Func<T, bool>> filter = null,
            int batchSize = 100,
            CancellationToken cancellation = default
            ) where T : IEntity

            => new Watcher<T, TResult>(eventTypes, projection, filter, batchSize, cancellation);
    }
}
