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
        /// Will be true when the change-stream is active and receiving changes.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Returns true if watching can be restarted if it's stopped due to an error or invalidate event. 
        /// Will always return false after cancellation is requested via the cancellation token.
        /// </summary>
        public bool CanRestart { get => !token.IsCancellationRequested; }

        private readonly PipelineDefinition<ChangeStreamDocument<T>, ChangeStreamDocument<T>> pipeline;
        private readonly ChangeStreamOptions options;
        private CancellationToken token;

        public Watcher() => throw new NotSupportedException("Please use DB.Watch<T>() to instantiate this class!");

        internal Watcher(EventType eventTypes, int batchSize, CancellationToken cancellation)
        {
            token = cancellation;

            var ops = new HashSet<ChangeStreamOperationType>();

            if (eventTypes.HasFlag(EventType.Created))
                ops.Add(ChangeStreamOperationType.Insert);

            if (eventTypes.HasFlag(EventType.Updated))
            {
                ops.Add(ChangeStreamOperationType.Update);
                ops.Add(ChangeStreamOperationType.Replace);
            }

            if (eventTypes.HasFlag(EventType.Deleted))
                ops.Add(ChangeStreamOperationType.Delete);

            pipeline = new IPipelineStageDefinition[] {
                PipelineStageDefinitionBuilder.Match(
                    Builders<ChangeStreamDocument<T>>.Filter.Where(
                        x => ops.Contains(x.OperationType)))
            };

            options = new ChangeStreamOptions
            {
                BatchSize = batchSize,
                FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
                MaxAwaitTime = TimeSpan.FromSeconds(10)
            };

            StartWatching();
        }

        /// <summary>
        /// If the watcher is not active (due to an error or invalidate event), you can try to restart the watching again with this method.
        /// </summary>
        /// <param name="dontResume">Set this to true if you don't want to resume the change-stream and start processing new changes only</param>
        public void ReStart(bool dontResume = false)
        {
            if (!CanRestart)
                throw new InvalidOperationException(
                    "This watcher has been aborted/cancelled. " +
                    "The subscribers have already been purged. " +
                    "Please instantiate a new watcher and subscribe to the events again.");

            if (!IsActive)
            {
                if (dontResume)
                    options.StartAfter = default;

                StartWatching();
            }
        }

        private void StartWatching()
        {
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    using (var cursor = DB.Collection<T>().Watch(pipeline, options))
                    {
                        IsActive = true;

                        while (!token.IsCancellationRequested && await cursor.MoveNextAsync())
                        {
                            if (cursor.Current.Any())
                            {
                                options.StartAfter = cursor.Current.Last().ResumeToken;
                                OnChanges?.Invoke(cursor.Current.Select(x => x.FullDocument));
                            }
                        }

                        OnStop?.Invoke();

                        if (token.IsCancellationRequested)
                        {
                            if (OnChanges != null)
                                foreach (Action<IEnumerable<T>> a in OnChanges.GetInvocationList())
                                    OnChanges -= a;

                            if (OnError != null)
                                foreach (Action<Exception> a in OnError.GetInvocationList())
                                    OnError -= a;

                            if (OnStop != null)
                                foreach (Action a in OnStop.GetInvocationList())
                                    OnStop -= a;
                        }
                    }
                }
                catch (Exception x)
                {
                    OnError?.Invoke(x);
                }
                finally
                {
                    IsActive = false;
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
