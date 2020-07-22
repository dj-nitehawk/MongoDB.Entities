using MongoDB.Entities.Core;
using System;
using System.Threading;

namespace MongoDB.Entities
{
    public partial class DB
    {
        /// <summary>
        /// Instantiates a watcher thread that will open up a mongodb change stream. 
        /// Subscribe to the events supplied by this class in order to perform an action when changes occur.
        /// </summary>
        /// <typeparam name="T">The entity type to watch for changes</typeparam>
        /// <param name="eventTypes">Specify which type of event to watch for. You can specify more than one type like: EventType.Created | EventType.Updated | EventType.Deleted</param>
        /// <param name="filter">Specify a filter so that change event is fired only if the received entities match this criteria</param>
        /// <param name="batchSize">The max number of entities to receive for a single event occurence</param>
        /// <param name="cancellation">A cancellation token for ending the watch/ change stream</param>
        public static Watcher<T> Watch<T>(EventType eventTypes, Func<T, bool> filter = null, int batchSize = 100, CancellationToken cancellation = default) where T : IEntity
            => new Watcher<T>(eventTypes, filter, batchSize, cancellation);
    }
}
