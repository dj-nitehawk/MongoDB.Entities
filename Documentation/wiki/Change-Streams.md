## Change-streams
Change-stream support is provided via the `db.Watcher<T>` registry. You can use a watcher to receive notifications when a given entity type gets either created, updated or deleted. Only monitoring at the collection level is supported.

### 1. Retrieve a watcher instance
```csharp
var watcher = db.Watcher<Author>("some-unique-name-for-the-watcher");
```
Pass a unique string to get a watcher instance. If a watcher by that name already exists in the registry, that instance will be returned. If no such watcher exists, a fresh watcher will be returned.

### 2. Configure and start the watcher
```csharp
watcher.Start(
    eventTypes: EventType.Created | EventType.Updated | EventType.Deleted,
    filter: null,
    batchSize: 25,
    onlyGetIDs: false,
    autoResume: true,
    cancellation: default);
```
> [!note]
> All except the eventTypes parameter are optional and the default values are shown above.

**eventTypes:** Specify what kind of change event you'd like to watch for. Multiple types can be specified as shown.

**filter:** If you'd like to receive only a subset of change events, you can do so by supplying a lambda expression to this parameter. For example, if you're interested in being notified about changes to Authors who are aged 25 and above, set the filter to the following:
```csharp
x => x.FullDocument.Age >= 25
```
> [!note]
> Filtering cannot be done if the types of change you're interested in includes deletions. Because the entity data no longer exists in the database when a deletion occurs and mongodb only returns the entity ID with the change event.

**batchSize:** Specify the maximum number of entities you'd like to receive per change notification/ a single event firing. The default is 25.

**onlyGetIDs:** Set to true if you don't want the complete entity details. In which case all properties except the ID will be null on the received entities.

**autoResume:** Change-streams will be auto-resumed by default unless you set this parameter to false. What that means is, say for example you start a watcher and after a while the watcher stops due to an error or an [invalidate event](https://docs.mongodb.com/manual/reference/change-events/#invalidate-event). You can then re-start the watcher and it will start receiving notifications from where it left off and you won't lose any changes that occurred while the watcher was stopped. If you set this to false, then those changes are skipped and only new changes are received. *The resume tokens are not automatically stored on disk by the library. [See here](Change-Streams.md#resuming-across-app-restarts) about resuming across app restarts*.

**cancellation:** If you'd like to cancel/abort a watcher and close the change-stream permanently at a future time, pass a cancellation token to this parameter.

### 3. Subscribe to the events
#### OnChanges
```csharp
watcher.OnChanges += authors =>
{
    foreach (var author in authors)
    {
        Console.WriteLine("received: " + author.Name);
    }
};
```
This event is fired when desired change events have been received from mongodb. For the above example, when author entities have been either created, updated or deleted, this event/action will receive those entities in batches. You can access the received entities via the input action parameter called `authors`.

**Receiving ChangeStreamDocuments:**

If you'd like to receive the complete `ChangeStreamDocuments` instead of just the entities, you can subscribe to the `OnChangesCSD` method like so:
```csharp
watcher.OnChangesCSD += changes =>
{
    foreach (var csd in changes)
    {
        Console.WriteLine(
            "Removed Fields: " +
            string.Join(", ", csd.UpdateDescription.RemovedFields));
    }
};
```

**Async event handlers:**

There's also the async variants of these events called `OnChangesAsync` and `OnChangesCSDAsync` for when you need to do IO bound work inside the handler in the correct batch order.

What that means is, if you do `watcher.OnChanges += async authors => {...}` the handler function will be called in parallel for each batch of change events. It is ok to use it when the order of the batches are not important to you or when you don't need [precise resume token retrieval](Change-Streams.md#precise-resume-token-retrieval).

As a rule of thumb, always do `watcher.OnChangesAsync += async authors => {...}` when you need to use the `await` keyword inside the event handler.

> [see here](https://gist.github.com/dj-nitehawk/dc87f368746cb8666b18cc00dd5ecf88) for a full example.

#### OnError
```csharp
watcher.OnError += exception =>
{
    Console.WriteLine("error: " + exception.Message);

    if (watcher.CanRestart)
    {
        watcher.ReStart();
        Console.WriteLine("Watching restarted!");
    }
};
```
In case the change-stream ends due to an error, the `OnError` event will be fired with the exception. You can try to restart the watcher as shown above.

#### OnStop
```csharp
watcher.OnStop += () =>
{
    Console.WriteLine("Watching stopped!");

    if (watcher.CanRestart)
    {
        watcher.ReStart();
        Console.WriteLine("Watching restarted!");
    }
    else
    {
        Console.WriteLine("This watcher is dead!");
    }
};
```
This event will be fired when the internal cursor gets closed due to either you requesting cancellation or an `invalidate` event occurring such as renaming or dropping of the watched collection.

If the cause of stopping is due to aborting via cancellation-token, the watcher has already purged all the event subscribers and no longer can be restarted.

If the cause was an `invalidate` event, you can restart watching as shown above. The existing event subscribers will continue to receive change events.

## Limiting properties of returned entities
You can apply a projection in order to specify which properties of your entity type you'd like returned when the change events are triggered like so:
```csharp
watcher.Start(
    eventTypes: EventType.Created | EventType.Updated,
    projection: a => new Author
    {
        Name = a.Name,
        Address = a.Address
    });
```
With the above example, only the author name and address properties will have their values populated. The rest of the properties will be null.

> [!note]
> Projections cannot be done if the types of change you're interested in includes deletions. Because the entity data no longer exists in the database when a deletion occurs and mongodb only returns the entity ID with the change event.


## Resuming across app restarts
You can retrieve a resume token from the `ResumeToken` property of the watcher like so:
```csharp
var token = watcher.ResumeToken;
```
Persist this token to a non-volatile medium and use it upon app startup to initiate a watcher to continue/resume from where the app left off last time it was running.
```csharp
watcher.StartWithToken(token, ...);
```

> [see here](https://gist.github.com/dj-nitehawk/dc87f368746cb8666b18cc00dd5ecf88) for a full example of how to use resume tokens.


### Precise resume token retrieval
The resume token returned by the `watcher.ResumeToken` property is the last token of the current batch of change events. If your app/server is prone to frequent crashes or your app tends to get shut down abruptly (without letting all the `OnChanges*` event handlers complete their work), you may lose some change events when you resume watching with the last token retrieved from `watcher.ResumeToken`.

To prevent that from happening and have fine-grain control of the token storage and resumption, you must subscribe to a `OnChangesCSD*` event and retrieve + store the token from each `ChangeStreamDocument` like so:

```csharp
watcher.OnChangesCSDAsync += async csDocs =>
{
    foreach (var csd in csDocs)
    {
        if (csd.OperationType == ChangeStreamOperationType.Insert)
        {
            Console.WriteLine("created: " + csd.FullDocument.Title);
        }
        await StoreResumeTokenAsync(csd.ResumeToken);
    }
};
```

If you're re-starting a stopped/errored watcher, you can provide the latest resume token you have like so:
```csharp
watcher.OnError += exception =>
{
    Console.WriteLine("error: " + exception.Message);

    if (watcher.CanRestart)
    {
        watcher.ReStart(lastResumeToken);
    }
};
```

## Access all watchers in the registry
```csharp
var watchers = db.Watchers<Author>();

foreach (var w in watchers)
{
    Console.WriteLine("watcher: " + w.Name);
}
```
> [!note]
> There's a watcher registry per entity type and the watcher names need only be unique to each registry.

## Notes on resource usage
Each watcher/change-stream you create opens a long-running cursor on the database server, which also means a persistent network connection between your application and the database. If you create more than a handful of watchers in your application, you should consider increasing the size of the mongodb driver thread-pool size as shown below:

```csharp
await DB.InitAsync("DatabaseName", new MongoClientSettings()
{
    ...
    MaxConnectionPoolSize = 100 + NumberOfWatchers,
    ...
});
```

In addition to persistent network connections/cursors, each watcher will use a small amount of memory for an async/await state machine that does the actual work of iterating the change-stream cursor and emitting events without blocking threads during IO.

The bottom line is, change-streams can be a double-edged sword if not used sparingly. The beefier the machine that runs your app, the more change-streams you can create without affecting the performance of the rest of your application.