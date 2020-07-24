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
        /// This event is fired when an exception is thrown in the change stream
        /// </summary>
        public event Action<Exception> OnError;

        /// <summary>
        /// This event is fired when the watching is aborted via the cancellation token
        /// </summary>
        public event Action OnAbort;

        /// <summary>
        /// Will be true when the change-stream is active and receiving changes
        /// </summary>
        public bool IsActive { get; private set; }

        private PipelineDefinition<ChangeStreamDocument<T>, ChangeStreamDocument<T>> pipeline;
        private ChangeStreamOptions options;
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

            var matchStage = PipelineStageDefinitionBuilder.Match(
                Builders<ChangeStreamDocument<T>>.Filter.Where(x => ops.Contains(x.OperationType)));

            pipeline = new IPipelineStageDefinition[] { matchStage };

            options = new ChangeStreamOptions
            {
                BatchSize = batchSize,
                FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
                MaxAwaitTime = TimeSpan.FromSeconds(10)
            };

            StartWatching();
        }

        /// <summary>
        /// If the watcher is not active (due to error), you can try to restart the watching again with this method.
        /// </summary>
        public void ReStart()
        {
            if (token.IsCancellationRequested)
                throw new InvalidOperationException(
                    "This watcher has already been aborted/cancelled. " +
                    "The subscribers have been purged. " +
                    "Please instantiate a new watcher and subscribe to the events again!");

            if (!IsActive)
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
                        IsActive = true;

                        while (!token.IsCancellationRequested && await cursor.MoveNextAsync())
                        {
                            if (cursor.Current.Any())
                                OnChanges?.Invoke(cursor.Current.Select(x => x.FullDocument));
                        }

                        StopWatching();
                    }
                }
                catch (Exception x)
                {
                    IsActive = false;
                    OnError?.Invoke(x);
                }
            });
        }

        private void StopWatching()
        {
            IsActive = false;

            OnAbort?.Invoke();

            if (OnChanges != null)
                foreach (Action<IEnumerable<T>> a in OnChanges.GetInvocationList())
                    OnChanges -= a;

            if (OnError != null)
                foreach (Action<Exception> a in OnError.GetInvocationList())
                    OnError -= a;

            if (OnAbort != null)
                foreach (Action a in OnAbort.GetInvocationList())
                    OnAbort -= a;
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
