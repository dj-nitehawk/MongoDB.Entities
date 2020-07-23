using MongoDB.Entities.Core;
using System.Threading;

namespace MongoDB.Entities
{
    public partial class DB
    {
        /// <summary>
        /// Instantiates a watcher thread that will open up a mongodb change stream. 
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="eventTypes">Type of event to watch for. Multiple can be specified as: EventType.Created | EventType.Updated | EventType.Deleted</param>
        /// <param name="batchSize">The max number of entities to receive for a single event occurence</param>
        /// <param name="cancellation">A cancellation token for ending the watch/ change stream</param>
        public static Watcher<T> Watch<T>(
            EventType eventTypes,
            int batchSize = 25,
            CancellationToken cancellation = default
            ) where T : IEntity

            => new Watcher<T>(eventTypes, batchSize, cancellation);
    }
}
