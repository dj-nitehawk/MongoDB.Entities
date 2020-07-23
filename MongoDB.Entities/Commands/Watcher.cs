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
        /// This event is fired when the desired types of events has occured. Will have a list of entities that was received as input.
        /// </summary>
        public event Action<IEnumerable<T>> OnEvents;

        /// <summary>
        /// This event is fired when an exception is thrown in the change stream
        /// </summary>
        public event Action<Exception> OnError;

        /// <summary>
        /// This event is fired when the watching is aborted via the cancellation token
        /// </summary>
        public event Action OnAbort;

        public Watcher() => throw new NotSupportedException("Please use DB.Watch<T>() to instantiate this class!");

        internal Watcher(EventType eventTypes, int batchSize, CancellationToken cancellation)
        {
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

            PipelineDefinition<ChangeStreamDocument<T>, ChangeStreamDocument<T>> pipeline =
                new IPipelineStageDefinition[] { matchStage };

            var options = new ChangeStreamOptions
            {
                BatchSize = batchSize,
                FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
                MaxAwaitTime = TimeSpan.FromSeconds(10)
            };

            Task.Factory.StartNew(async () =>
            {
                try
                {
                    using (var cursor = DB.Collection<T>().Watch(pipeline, options))
                    {
                        while (!cancellation.IsCancellationRequested && await cursor.MoveNextAsync(cancellation))
                        {
                            if (cursor.Current.Any())
                                OnEvents?.Invoke(cursor.Current.Select(x => x.FullDocument));
                        }

                        OnAbort?.Invoke();
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
