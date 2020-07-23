using MongoDB.Driver;
using MongoDB.Entities.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace MongoDB.Entities
{
    /// <summary>
    /// Watcher for subscribing to mongodb change streams
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <typeparam name="TResult">The projected type of the end result</typeparam>
    public class Watcher<T, TResult> where T : IEntity
    {
        /// <summary>
        /// This event is fired when the desired type of change has occured. Will have a list of entities that was received as input.
        /// </summary>
        public event Action<IEnumerable<TResult>> ChangesReceived;

        /// <summary>
        /// This event is fired when an exception is thrown in the change stream
        /// </summary>
        public event Action<Exception> ErrorOccurred;

        /// <summary>
        /// This event is fired when the change stream ends
        /// </summary>
        public event Action ChangeStreamEnded;

        public Watcher() => throw new NotSupportedException("Please use DB.Watch<T>() to instantiate this class!");

        internal Watcher(EventType eventTypes, Expression<Func<T, bool>> filter = null, Expression<Func<T, TResult>> projection = null, int batchSize = 100, CancellationToken cancellation = default)
        {
            var ops = new List<ChangeStreamOperationType>();

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

            var replaceWithStage = PipelineStageDefinitionBuilder.ReplaceWith<ChangeStreamDocument<T>, T>(x => x.FullDocument);

            var matchStage2 = PipelineStageDefinitionBuilder.Match(Builders<T>.Filter.Where(filter));

            var projectStage = PipelineStageDefinitionBuilder.Project(projection);

            PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline =
                new IPipelineStageDefinition[] { matchStage, replaceWithStage, matchStage2, projectStage };

            var options = new ChangeStreamOptions
            {
                BatchSize = batchSize,
                FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
            };

            using (var cursor = DB.Collection<T>().Watch(pipeline, options))
            {
                IEnumerable<TResult> docs;

                try
                {
                    while (cursor.MoveNext(cancellation))
                    {
                        docs = cursor.Current;

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
