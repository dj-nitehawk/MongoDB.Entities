using MongoDB.Driver;
using MongoDB.Entities.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// Watcher for subscribing to mongodb change streams.
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    public class Watcher<T> where T : IEntity
    {
        /// <summary>
        /// This event is fired when the desired types of events have occured. Will have a list of entities that was received as input.
        /// </summary>
        public event Action<IEnumerable<T>> OnChanges;

        /// <summary>
        /// This event is fired when an exception is thrown in the change-stream.
        /// </summary>
        public event Action<Exception> OnError;

        /// <summary>
        /// This event is fired when the internal cursor get closed due to an 'invalidate' event or cancellation is requested via the cancellation token.
        /// </summary>
        public event Action OnStop;

        /// <summary>
        /// The name of this watcher instance
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns true if watching can be restarted if it's stopped due to an error or invalidate event. 
        /// Will always return false after cancellation is requested via the cancellation token.
        /// </summary>
        public bool CanRestart { get => !cancelToken.IsCancellationRequested; }

        private PipelineDefinition<ChangeStreamDocument<T>, ChangeStreamDocument<T>> pipeline;
        private ChangeStreamOptions options;
        private CancellationToken cancelToken;
        private bool started;

        internal Watcher(string name) => Name = name;

        /// <summary>
        /// Starts the watcher instance with the supplied configuration
        /// </summary>
        /// <param name="eventTypes">Type of event to watch for. Multiple can be specified as: EventType.Created | EventType.Updated | EventType.Deleted</param>
        /// <param name="batchSize">The max number of entities to receive for a single event occurence</param>
        /// <param name="onlyGetIDs">Set this to true if you don't want the complete entity details. All properties except the ID will then be null.</param>
        /// <param name="cancellation">A cancellation token for ending the watch/ change stream</param>
        public void Start(EventType eventTypes, int batchSize = 25, bool onlyGetIDs = false, CancellationToken cancellation = default)
        {
            if (started)
                throw new InvalidOperationException("This watcher has already been initialized!");

            cancelToken = cancellation;

            var ops = new HashSet<ChangeStreamOperationType>();

            if ((eventTypes & EventType.Created) != 0)
                ops.Add(ChangeStreamOperationType.Insert);

            if ((eventTypes & EventType.Updated) != 0)
            {
                ops.Add(ChangeStreamOperationType.Update);
                ops.Add(ChangeStreamOperationType.Replace);
            }

            if ((eventTypes & EventType.Deleted) != 0)
                ops.Add(ChangeStreamOperationType.Delete);

            pipeline = new IPipelineStageDefinition[] {

                PipelineStageDefinitionBuilder.Match(
                    Builders<ChangeStreamDocument<T>>.Filter.Where(
                        x => ops.Contains(x.OperationType))),

                PipelineStageDefinitionBuilder.Project<ChangeStreamDocument<T>,ChangeStreamDocument<T>>(
                    $"{{ _id: 1, fullDocument: {(onlyGetIDs ? "'$documentKey'" : "1")} }}")
            };

            options = new ChangeStreamOptions
            {
                BatchSize = batchSize,
                FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
                MaxAwaitTime = TimeSpan.FromSeconds(10)
            };

            StartWatching();

            started = true;
        }

        /// <summary>
        /// If the watcher stopped due to an error or invalidate event, you can try to restart the watching again with this method.
        /// </summary>
        public void ReStart()
        {
            if (!CanRestart)
            {
                throw new InvalidOperationException(
                    "This watcher has been aborted/cancelled. " +
                    "The subscribers have already been purged. " +
                    "Please instantiate a new watcher and subscribe to the events again.");
            }

            StartWatching();
        }

        private void StartWatching()
        {
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    using (var cursor = DB.Collection<T>().Watch(pipeline, options))
                    {
                        while (!cancelToken.IsCancellationRequested && await cursor.MoveNextAsync())
                        {
                            if (cursor.Current.Any())
                                OnChanges?.Invoke(cursor.Current.Select(x => x.FullDocument));
                        }

                        OnStop?.Invoke();

                        if (cancelToken.IsCancellationRequested)
                        {
                            if (OnChanges != null)
                            {
                                foreach (Action<IEnumerable<T>> a in OnChanges.GetInvocationList())
                                    OnChanges -= a;
                            }

                            if (OnError != null)
                            {
                                foreach (Action<Exception> a in OnError.GetInvocationList())
                                    OnError -= a;
                            }

                            if (OnStop != null)
                            {
                                foreach (Action a in OnStop.GetInvocationList())
                                    OnStop -= a;
                            }
                        }
                    }
                }
                catch (Exception x)
                {
                    OnError?.Invoke(x);
                }
            }, TaskCreationOptions.LongRunning);
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
