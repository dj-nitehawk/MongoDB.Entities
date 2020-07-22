using MongoDB.Driver;
using MongoDB.Entities.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MongoDB.Entities
{
    /// <summary>
    /// Watcher for subscribing to mongodb change streams
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    public class Watcher<T> where T : IEntity
    {
        /// <summary>
        /// This event is fired when the desired type of change has occured. Will have a list of entities that was received as input.
        /// </summary>
        public event Action<IEnumerable<T>> ChangesReceived;

        /// <summary>
        /// This event is fired when an exception is thrown in the change stream
        /// </summary>
        public event Action<Exception> ErrorOccurred;

        /// <summary>
        /// This event is fired when the change stream ends
        /// </summary>
        public event Action ChangeStreamEnded;

        public Watcher() => throw new NotSupportedException("Please use DB.Watch<T>() to instantiate this class!");

        internal Watcher(EventType eventTypes, Func<T, bool> filter = null, int batchSize = 100, CancellationToken cancellation = default)
        {
            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<T>>();

            if (eventTypes.HasFlag(EventType.Created))
                pipeline.Match(x => x.OperationType == ChangeStreamOperationType.Insert);

            if (eventTypes.HasFlag(EventType.Updated))
                pipeline.Match(x => x.OperationType == ChangeStreamOperationType.Update || x.OperationType == ChangeStreamOperationType.Replace);

            if (eventTypes.HasFlag(EventType.Deleted))
                pipeline.Match(x => x.OperationType == ChangeStreamOperationType.Delete);

            var options = new ChangeStreamOptions
            {
                BatchSize = batchSize,
                FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
            };

            using (var cursor = DB.Collection<T>().Watch(pipeline, options))
            {
                IEnumerable<T> docs;

                try
                {
                    while (cursor.MoveNext(cancellation))
                    {
                        docs = cursor.Current
                            .Select(csd => csd.FullDocument);

                        if (filter != null)
                            docs = docs.Where(filter);

                        if (docs.Any())
                            ChangesReceived?.Invoke(docs);
                    }
                }
                catch (Exception x)
                {
                    ErrorOccurred?.Invoke(x);
                }
                finally
                {
                    ChangeStreamEnded?.Invoke();
                }
            }

        }
    }

    [Flags]
    public enum EventType
    {
        Created = 1 << 1,
        Updated = 1 << 2,
        Deleted = 1 << 3
    }
}
