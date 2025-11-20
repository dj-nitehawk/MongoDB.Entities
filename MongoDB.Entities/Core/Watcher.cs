using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.Entities;

public delegate Task AsyncEventHandler<in TEventArgs>(TEventArgs args);

public static class AsyncEventHandlerExtensions
{
    extension<TEventArgs>(AsyncEventHandler<TEventArgs> handler)
    {
        public IEnumerable<AsyncEventHandler<TEventArgs>> GetHandlers()
            => handler.GetInvocationList().Cast<AsyncEventHandler<TEventArgs>>();

        public Task InvokeAllAsync(TEventArgs args)
            => Task.WhenAll(handler.GetHandlers().Select(h => h(args)));
    }
}

/// <summary>
/// Watcher for subscribing to mongodb change streams.
/// </summary>
/// <typeparam name="T">The type of entity</typeparam>
public class Watcher<T> where T : IEntity
{
    /// <summary>
    /// This event is fired when the desired types of events have occured. Will have a list of 'entities' that was received as input.
    /// </summary>
    public event Action<IEnumerable<T>>? OnChanges;

    /// <summary>
    /// This event is fired when the desired types of events have occured. Will have a list of 'entities' that was received as input.
    /// </summary>
    public event AsyncEventHandler<IEnumerable<T>>? OnChangesAsync;

    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// This event is fired when the desired types of events have occured. Will have a list of 'ChangeStreamDocuments' that was received as input.
    /// </summary>
    public event Action<IEnumerable<ChangeStreamDocument<T>>>? OnChangesCSD;

    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// This event is fired when the desired types of events have occured. Will have a list of 'ChangeStreamDocuments' that was received as input.
    /// </summary>
    public event AsyncEventHandler<IEnumerable<ChangeStreamDocument<T>>>? OnChangesCSDAsync;

    /// <summary>
    /// This event is fired when an exception is thrown in the change-stream.
    /// </summary>
    public event Action<Exception>? OnError;

    /// <summary>
    /// This event is fired when the internal cursor get closed due to an 'invalidate' event or cancellation is requested via the cancellation token.
    /// </summary>
    public event Action? OnStop;

    /// <summary>
    /// The name of this watcher instance
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Indicates whether this watcher has already been initialized or not.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Returns true if watching can be restarted if it was stopped due to an error or invalidate event.
    /// Will always return false after cancellation is requested via the cancellation token.
    /// </summary>
    public bool CanRestart => !_cancelToken.IsCancellationRequested;

    /// <summary>
    /// The last resume token received from mongodb server. Can be used to resume watching with .StartWithToken() method.
    /// </summary>
    public BsonDocument? ResumeToken => _options?.StartAfter;

    PipelineDefinition<ChangeStreamDocument<T>, ChangeStreamDocument<T>> _pipeline = null!;
    ChangeStreamOptions? _options;
    bool _resume;
    CancellationToken _cancelToken;
    readonly DB _db;

    internal Watcher(DB db, string name)
    {
        _db = db;
        Name = name;
    }

    /// <summary>
    /// Starts the watcher instance with the supplied parameters
    /// </summary>
    /// <param name="eventTypes">Type of event to watch for. Specify multiple like: EventType.Created | EventType.Updated | EventType.Deleted</param>
    /// <param name="filter">x => x.FullDocument.Prop1 == "SomeValue"</param>
    /// <param name="batchSize">The max number of entities to receive for a single event occurence</param>
    /// <param name="onlyGetIDs">Set to true if you don't want the complete entity details. All properties except the ID will then be null.</param>
    /// <param name="autoResume">
    /// Set to false if you'd like to skip the changes that happened while the watching was stopped. This will also make you unable to
    /// retrieve a ResumeToken.
    /// </param>
    /// <param name="cancellation">A cancellation token for ending the watching/change stream</param>
    public void Start(EventType eventTypes,
                      Expression<Func<ChangeStreamDocument<T>, bool>>? filter = null,
                      int batchSize = 25,
                      bool onlyGetIDs = false,
                      bool autoResume = true,
                      CancellationToken cancellation = default)
        => Init(null, eventTypes, filter, null, batchSize, onlyGetIDs, autoResume, cancellation);

    /// <summary>
    /// Starts the watcher instance with the supplied parameters. Supports projection.
    /// </summary>
    /// <param name="eventTypes">Type of event to watch for. Specify multiple like: EventType.Created | EventType.Updated | EventType.Deleted</param>
    /// <param name="projection">A projection expression for the entity</param>
    /// <param name="filter">x => x.FullDocument.Prop1 == "SomeValue"</param>
    /// <param name="batchSize">The max number of entities to receive for a single event occurence</param>
    /// <param name="autoResume">
    /// Set to false if you'd like to skip the changes that happened while the watching was stopped. This will also make you unable to
    /// retrieve a ResumeToken.
    /// </param>
    /// <param name="cancellation">A cancellation token for ending the watching/change stream</param>
    public void Start(EventType eventTypes,
                      Expression<Func<T, T>> projection,
                      Expression<Func<ChangeStreamDocument<T>, bool>>? filter = null,
                      int batchSize = 25,
                      bool autoResume = true,
                      CancellationToken cancellation = default)
        => Init(null, eventTypes, filter, projection, batchSize, false, autoResume, cancellation);

    /// <summary>
    /// Starts the watcher instance with the supplied parameters
    /// </summary>
    /// <param name="eventTypes">Type of event to watch for. Specify multiple like: EventType.Created | EventType.Updated | EventType.Deleted</param>
    /// <param name="filter">b => b.Eq(d => d.FullDocument.Prop1, "value")</param>
    /// <param name="batchSize">The max number of entities to receive for a single event occurence</param>
    /// <param name="onlyGetIDs">Set to true if you don't want the complete entity details. All properties except the ID will then be null.</param>
    /// <param name="autoResume">
    /// Set to false if you'd like to skip the changes that happened while the watching was stopped. This will also make you unable to
    /// retrieve a ResumeToken.
    /// </param>
    /// <param name="cancellation">A cancellation token for ending the watching/change stream</param>
    public void Start(EventType eventTypes,
                      Func<FilterDefinitionBuilder<ChangeStreamDocument<T>>, FilterDefinition<ChangeStreamDocument<T>>> filter,
                      int batchSize = 25,
                      bool onlyGetIDs = false,
                      bool autoResume = true,
                      CancellationToken cancellation = default)
        => Init(null, eventTypes, filter(Builders<ChangeStreamDocument<T>>.Filter), null, batchSize, onlyGetIDs, autoResume, cancellation);

    /// <summary>
    /// Starts the watcher instance with the supplied parameters. Supports projection.
    /// </summary>
    /// <param name="eventTypes">Type of event to watch for. Specify multiple like: EventType.Created | EventType.Updated | EventType.Deleted</param>
    /// <param name="projection">A projection expression for the entity</param>
    /// <param name="filter">b => b.Eq(d => d.FullDocument.Prop1, "value")</param>
    /// <param name="batchSize">The max number of entities to receive for a single event occurence</param>
    /// <param name="autoResume">
    /// Set to false if you'd like to skip the changes that happened while the watching was stopped. This will also make you unable to
    /// retrieve a ResumeToken.
    /// </param>
    /// <param name="cancellation">A cancellation token for ending the watching/change stream</param>
    public void Start(EventType eventTypes,
                      Expression<Func<T, T>> projection,
                      Func<FilterDefinitionBuilder<ChangeStreamDocument<T>>, FilterDefinition<ChangeStreamDocument<T>>> filter,
                      int batchSize = 25,
                      bool autoResume = true,
                      CancellationToken cancellation = default)
        => Init(null, eventTypes, filter(Builders<ChangeStreamDocument<T>>.Filter), projection, batchSize, false, autoResume, cancellation);

    /// <summary>
    /// Starts the watcher instance with the supplied configuration
    /// </summary>
    /// <param name="resumeToken">A resume token to start receiving changes after some point back in time</param>
    /// <param name="eventTypes">Type of event to watch for. Specify multiple like: EventType.Created | EventType.Updated | EventType.Deleted</param>
    /// <param name="filter">x => x.FullDocument.Prop1 == "SomeValue"</param>
    /// <param name="batchSize">The max number of entities to receive for a single event occurence</param>
    /// <param name="onlyGetIDs">Set to true if you don't want the complete entity details. All properties except the ID will then be null.</param>
    /// <param name="cancellation">A cancellation token for ending the watching/change stream</param>
    public void StartWithToken(BsonDocument resumeToken,
                               EventType eventTypes,
                               Expression<Func<ChangeStreamDocument<T>, bool>>? filter = null,
                               int batchSize = 25,
                               bool onlyGetIDs = false,
                               CancellationToken cancellation = default)
        => Init(resumeToken, eventTypes, filter, null, batchSize, onlyGetIDs, true, cancellation);

    /// <summary>
    /// Starts the watcher instance with the supplied configuration. Supports projection.
    /// </summary>
    /// <param name="resumeToken">A resume token to start receiving changes after some point back in time</param>
    /// <param name="eventTypes">Type of event to watch for. Specify multiple like: EventType.Created | EventType.Updated | EventType.Deleted</param>
    /// <param name="projection">A projection expression for the entity</param>
    /// <param name="filter">x => x.FullDocument.Prop1 == "SomeValue"</param>
    /// <param name="batchSize">The max number of entities to receive for a single event occurence</param>
    /// <param name="cancellation">A cancellation token for ending the watching/change stream</param>
    public void StartWithToken(BsonDocument resumeToken,
                               EventType eventTypes,
                               Expression<Func<T, T>> projection,
                               Expression<Func<ChangeStreamDocument<T>, bool>>? filter = null,
                               int batchSize = 25,
                               CancellationToken cancellation = default)
        => Init(resumeToken, eventTypes, filter, projection, batchSize, false, true, cancellation);

    /// <summary>
    /// Starts the watcher instance with the supplied configuration
    /// </summary>
    /// <param name="resumeToken">A resume token to start receiving changes after some point back in time</param>
    /// <param name="eventTypes">Type of event to watch for. Specify multiple like: EventType.Created | EventType.Updated | EventType.Deleted</param>
    /// <param name="filter">b => b.Eq(d => d.FullDocument.Prop1, "value")</param>
    /// <param name="batchSize">The max number of entities to receive for a single event occurence</param>
    /// <param name="onlyGetIDs">Set to true if you don't want the complete entity details. All properties except the ID will then be null.</param>
    /// <param name="cancellation">A cancellation token for ending the watching/change stream</param>
    public void StartWithToken(BsonDocument resumeToken,
                               EventType eventTypes,
                               Func<FilterDefinitionBuilder<ChangeStreamDocument<T>>, FilterDefinition<ChangeStreamDocument<T>>> filter,
                               int batchSize = 25,
                               bool onlyGetIDs = false,
                               CancellationToken cancellation = default)
        => Init(resumeToken, eventTypes, filter(Builders<ChangeStreamDocument<T>>.Filter), null, batchSize, onlyGetIDs, true, cancellation);

    /// <summary>
    /// Starts the watcher instance with the supplied configuration. Supports projection.
    /// </summary>
    /// <param name="resumeToken">A resume token to start receiving changes after some point back in time</param>
    /// <param name="eventTypes">Type of event to watch for. Specify multiple like: EventType.Created | EventType.Updated | EventType.Deleted</param>
    /// <param name="projection">A projection expression for the entity</param>
    /// <param name="filter">b => b.Eq(d => d.FullDocument.Prop1, "value")</param>
    /// <param name="batchSize">The max number of entities to receive for a single event occurence</param>
    /// <param name="cancellation">A cancellation token for ending the watching/change stream</param>
    public void StartWithToken(BsonDocument resumeToken,
                               EventType eventTypes,
                               Expression<Func<T, T>> projection,
                               Func<FilterDefinitionBuilder<ChangeStreamDocument<T>>, FilterDefinition<ChangeStreamDocument<T>>> filter,
                               int batchSize = 25,
                               CancellationToken cancellation = default)
        => Init(resumeToken, eventTypes, filter(Builders<ChangeStreamDocument<T>>.Filter), projection, batchSize, false, true, cancellation);

    void Init(BsonDocument? resumeToken,
              EventType eventTypes,
              FilterDefinition<ChangeStreamDocument<T>> filter,
              Expression<Func<T, T>>? projection,
              int batchSize,
              bool onlyGetIDs,
              bool autoResume,
              CancellationToken cancellation)
    {
        if (IsInitialized)
            throw new InvalidOperationException("This watcher has already been initialized!");

        _resume = autoResume;
        _cancelToken = cancellation;

        var ops = new List<ChangeStreamOperationType>(3) { ChangeStreamOperationType.Invalidate };

        if ((eventTypes & EventType.Created) != 0)
            ops.Add(ChangeStreamOperationType.Insert);

        if ((eventTypes & EventType.Updated) != 0)
        {
            ops.Add(ChangeStreamOperationType.Update);
            ops.Add(ChangeStreamOperationType.Replace);
        }

        if ((eventTypes & EventType.Deleted) != 0)
            ops.Add(ChangeStreamOperationType.Delete);

        if (ops.Contains(ChangeStreamOperationType.Delete))
        {
            if (filter != null)
            {
                throw new ArgumentException(
                    "Filtering is not supported when watching for deletions " +
                    "as the entity data no longer exists in the db " +
                    "at the time of receiving the event.");
            }

            if (projection != null)
            {
                throw new ArgumentException(
                    "Projecting is not supported when watching for deletions " +
                    "as the entity data no longer exists in the db " +
                    "at the time of receiving the event.");
            }
        }

        var filters = Builders<ChangeStreamDocument<T>>.Filter.Where(x => ops.Contains(x.OperationType));

        if (filter != null)
            filters &= filter;

        var stages = new List<IPipelineStageDefinition>(3)
        {
            PipelineStageDefinitionBuilder.Match(filters),
            PipelineStageDefinitionBuilder.Project<ChangeStreamDocument<T>, ChangeStreamDocument<T>>(
                """
                {
                    _id: 1,
                    operationType: 1,
                    documentKey: 1,
                    updateDescription: 1,
                    fullDocument: { $ifNull: ['$fullDocument', '$documentKey'] }
                }
                """)
        };

        if (projection != null)
            stages.Add(PipelineStageDefinitionBuilder.Project(BuildProjection(projection)));

        _pipeline = stages;

        _options = new()
        {
            StartAfter = resumeToken,
            BatchSize = batchSize,
            FullDocument = onlyGetIDs ? ChangeStreamFullDocumentOption.Default : ChangeStreamFullDocumentOption.UpdateLookup,
            MaxAwaitTime = TimeSpan.FromSeconds(10)
        };

        IsInitialized = true;

        StartWatching();
    }

    static ProjectionDefinition<ChangeStreamDocument<T>, ChangeStreamDocument<T>> BuildProjection(Expression<Func<T, T>> projection)
    {
        var rendered = Builders<T>.Projection
                                  .Expression(projection)
                                  .Render(new(BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry));

        BsonDocument doc = new()
        {
            { "_id", 1 },
            { "operationType", 1 },
            { "documentKey", 1 },
            { "updateDescription", 1 },
            { "fullDocument._id", 1 }
        };

        foreach (var element in rendered.Document.Elements)
        {
            if (element.Name == "_id")
                continue;

            var val = element.Value.ToString();
            doc["fullDocument." + element.Name] = val.Insert(1, "fullDocument.");
        }

        return doc;
    }

    /// <summary>
    /// If the watcher stopped due to an error or invalidate event, you can try to restart the watching again with this method.
    /// </summary>
    /// <param name="resumeToken">An optional resume token to restart watching with</param>
    public void ReStart(BsonDocument? resumeToken = null)
    {
        if (!CanRestart)
        {
            throw new InvalidOperationException(
                "This watcher has been aborted/cancelled. " +
                "The subscribers have already been purged. " +
                "Please instantiate a new watcher and subscribe to the events again.");
        }

        if (!IsInitialized)
            throw new InvalidOperationException("This watcher was never started. Please use .Start() first!");

        if (_cancelToken.IsCancellationRequested)
            throw new InvalidOperationException("This watcher cannot be restarted as it has been aborted/cancelled!");

        if (resumeToken != null && _options != null)
            _options.StartAfter = resumeToken;

        StartWatching();
    }

    void StartWatching()
    {
        //note  : don't use Task.Factory.StartNew with long-running option
        //reason: http://blog.i3arnon.com/2015/07/02/task-run-long-running/
        //        StartNew creates an unnecessary dedicated thread which gets released upon reaching first await.
        //        continuations will be run on different thread-pool threads upon re-entry.
        //        i.e. long-running thread creation is useless/wasteful for async delegates.

        _ = IterateCursorAsync();

        async Task IterateCursorAsync()
        {
            try
            {
                using var cursor = await _db.Collection<T>().WatchAsync(_pipeline, _options, _cancelToken).ConfigureAwait(false);

                while (!_cancelToken.IsCancellationRequested && await cursor.MoveNextAsync(_cancelToken).ConfigureAwait(false))
                {
                    if (!cursor.Current.Any())
                        continue;

                    if (_resume && _options != null)
                        _options.StartAfter = cursor.Current.Last().ResumeToken;

                    if (OnChangesAsync != null)
                    {
                        await OnChangesAsync.InvokeAllAsync(
                            cursor.Current
                                  .Where(d => d.OperationType != ChangeStreamOperationType.Invalidate)
                                  .Select(d => d.FullDocument)).ConfigureAwait(false);
                    }

                    OnChanges?.Invoke(
                        cursor.Current
                              .Where(d => d.OperationType != ChangeStreamOperationType.Invalidate)
                              .Select(d => d.FullDocument));

                    if (OnChangesCSDAsync != null)
                        await OnChangesCSDAsync.InvokeAllAsync(cursor.Current).ConfigureAwait(false);

                    OnChangesCSD?.Invoke(cursor.Current);
                }

                OnStop?.Invoke();

                if (_cancelToken.IsCancellationRequested)
                {
                    if (OnChangesAsync != null)
                    {
                        foreach (var h in OnChangesAsync.GetHandlers())
                            OnChangesAsync -= h;
                    }

                    if (OnChangesCSDAsync != null)
                    {
                        foreach (var h in OnChangesCSDAsync.GetHandlers())
                            OnChangesCSDAsync -= h;
                    }

                    if (OnChanges != null)
                    {
                        foreach (var a in OnChanges.GetInvocationList().Cast<Action<IEnumerable<T>>>())
                            OnChanges -= a;
                    }

                    if (OnChangesCSD != null)
                    {
                        // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                        foreach (Action<IEnumerable<ChangeStreamDocument<T>>> a in OnChangesCSD.GetInvocationList())
                            OnChangesCSD -= a;
                    }

                    if (OnError != null)
                    {
                        // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                        foreach (Action<Exception> a in OnError.GetInvocationList())
                            OnError -= a;
                    }

                    if (OnStop != null)
                    {
                        // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                        foreach (Action a in OnStop.GetInvocationList())
                            OnStop -= a;
                    }
                }
            }
            catch (Exception x)
            {
                OnError?.Invoke(x);
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