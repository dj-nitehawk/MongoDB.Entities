using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
        /// Returns true if watching can be restarted if it was stopped due to an error or invalidate event. 
        /// Will always return false after cancellation is requested via the cancellation token.
        /// </summary>
        public bool CanRestart { get => !cancelToken.IsCancellationRequested; }

        /// <summary>
        /// The last resume token received from mongodb server. Can be used to resume watching with .StartWithToken() method.
        /// </summary>
        public BsonDocument ResumeToken => options?.StartAfter;

        private PipelineDefinition<ChangeStreamDocument<T>, ChangeStreamDocument<T>> pipeline;
        private ChangeStreamOptions options;
        private bool resume;
        private CancellationToken cancelToken;
        private bool initialized;

        internal Watcher(string name) => Name = name;

        /// <summary>
        /// Starts the watcher instance with the supplied parameters
        /// </summary>
        /// <param name="eventTypes">Type of event to watch for. Specify multiple like: EventType.Created | EventType.Updated | EventType.Deleted</param>
        /// <param name="filter">x => x.FullDocument.Prop1 == "SomeValue"</param>
        /// <param name="batchSize">The max number of entities to receive for a single event occurence</param>
        /// <param name="onlyGetIDs">Set to true if you don't want the complete entity details. All properties except the ID will then be null.</param>
        /// <param name="autoResume">Set to false if you'd like to skip the changes that happened while the watching was stopped. This will also make you unable to retrieve a ResumeToken.</param>
        /// <param name="cancellation">A cancellation token for ending the watching/change stream</param>
        public void Start(
            EventType eventTypes,
            Expression<Func<ChangeStreamDocument<T>, bool>> filter = null,
            int batchSize = 25,
            bool onlyGetIDs = false,
            bool autoResume = true,
            CancellationToken cancellation = default)
        => Init(null, eventTypes, filter, batchSize, onlyGetIDs, autoResume, cancellation);

        /// <summary>
        /// Starts the watcher instance with the supplied configuration
        /// </summary>
        /// <param name="resumeToken">A resume token to start receiving changes after some point back in time</param>
        /// <param name="eventTypes">Type of event to watch for. Specify multiple like: EventType.Created | EventType.Updated | EventType.Deleted</param>
        /// <param name="filter">x => x.FullDocument.Prop1 == "SomeValue"</param>
        /// <param name="batchSize">The max number of entities to receive for a single event occurence</param>
        /// <param name="onlyGetIDs">Set to true if you don't want the complete entity details. All properties except the ID will then be null.</param>
        /// <param name="cancellation">A cancellation token for ending the watching/change stream</param>
        public void StartWithToken(
            BsonDocument resumeToken,
            EventType eventTypes,
            Expression<Func<ChangeStreamDocument<T>, bool>> filter = null,
            int batchSize = 25,
            bool onlyGetIDs = false,
            CancellationToken cancellation = default)
        => Init(resumeToken, eventTypes, filter, batchSize, onlyGetIDs, true, cancellation);

        private void Init(
            BsonDocument resumeToken,
            EventType eventTypes,
            Expression<Func<ChangeStreamDocument<T>, bool>> filter,
            int batchSize,
            bool onlyGetIDs,
            bool autoResume,
            CancellationToken cancellation)
        {
            if (initialized)
                throw new InvalidOperationException("This watcher has already been initialized!");

            resume = autoResume;
            cancelToken = cancellation;

            var ops = new HashSet<ChangeStreamOperationType>() { ChangeStreamOperationType.Invalidate };

            if ((eventTypes & EventType.Created) != 0)
                ops.Add(ChangeStreamOperationType.Insert);

            if ((eventTypes & EventType.Updated) != 0)
            {
                ops.Add(ChangeStreamOperationType.Update);
                ops.Add(ChangeStreamOperationType.Replace);
            }

            if ((eventTypes & EventType.Deleted) != 0)
                ops.Add(ChangeStreamOperationType.Delete);

            if (ops.Contains(ChangeStreamOperationType.Delete) && filter != null)
            {
                throw new ArgumentException(
                    "Filtering is not supported when watching for deletions " +
                    "as the entity data no longer exists in the db " +
                    "at the time of receiving the event.");
            }

            var filters = Builders<ChangeStreamDocument<T>>.Filter.Where(x => ops.Contains(x.OperationType));

            if (filter != null)
                filters &= Builders<ChangeStreamDocument<T>>.Filter.Where(filter);

            pipeline = new IPipelineStageDefinition[] {

                PipelineStageDefinitionBuilder.Match(filters),

                PipelineStageDefinitionBuilder.Project<ChangeStreamDocument<T>,ChangeStreamDocument<T>>(@"
                {
                    _id: 1,
                    operationType: 1,
                    fullDocument: { $ifNull: ['$fullDocument', '$documentKey'] }
                }")
            };

            options = new ChangeStreamOptions
            {
                StartAfter = resumeToken,
                BatchSize = batchSize,
                FullDocument = onlyGetIDs ? ChangeStreamFullDocumentOption.Default : ChangeStreamFullDocumentOption.UpdateLookup,
                MaxAwaitTime = TimeSpan.FromSeconds(10)
            };

            initialized = true;

            StartWatching();
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

            if (!initialized)
                throw new InvalidOperationException("This watcher was never started. Please use .Start() first!");

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
                        while (!cancelToken.IsCancellationRequested && await cursor.MoveNextAsync().ConfigureAwait(false))
                        {
                            if (cursor.Current.Any())
                            {
                                if (cursor.Current.First().OperationType != ChangeStreamOperationType.Invalidate)
                                {
                                    if (resume) options.StartAfter = cursor.Current.Last().ResumeToken;
                                    OnChanges?.Invoke(cursor.Current.Select(x => x.FullDocument));
                                }
                                else if (resume)
                                {
                                    options.StartAfter = cursor.Current.First().ResumeToken;
                                }
                            }
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
