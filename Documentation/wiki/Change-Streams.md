## Change-streams
change-stream support is provided via the `DB.Watcher<T>` registry. you can use a watcher to receive notifications when a given entity type gets either created, updated or deleted. only monitoring at the collection level is supported.

### 1. Retrieve a watcher instance
```csharp
var watcher = DB.Watcher<Author>("some-unique-name-for-the-watcher");
```
pass a unique string to get a watcher instance. if a watcher by that name already exists in the registry, that instance will be returned. if no such watcher exists, a fresh watcher will be returned.

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
> all except the eventTypes parameter are optional and the default values are shown above.

**eventTypes:** specify what kind of change event you'd like to watch for. multiple types can be specified as shown.

**filter:** if you'd like to receive only a subset of change events, you can do so by supplying a lambda expression to this parameter. for example, if you're interesed in being notified about changes to Authors who are aged 25 and above, set the filter to the following:
```csharp
x => x.FullDocument.Age >= 25
```
> [!note]
> filtering cannot be done if the types of change you're interested in includes deletions. because the entity data no longer exists in the database when a deletion occurs and mongodb only returns the entity ID with the change event.

**batchSize:** specify the maximum number of entities you'd like to receive per change notificatin/ a single event firing. the default is 25.

**onlyGetIDs:** set to true if you don't want the complete entity details. in which case all properties except the ID will be null on the received entities.

**autoResume:** change-streams will be auto-resumed by default unless you set this parameter to false. what that means is, say for example you start a watcher and after a while the watcher stops due to an error or an [invalidate event](https://docs.mongodb.com/manual/reference/change-events/#invalidate-event). you can then re-start the watcher and it will start receiving notifications from where it left off and you won't lose any changes that occurred while the watcher was stopped. if you set this to false, then those changes are skipped and only new changes are received.

**cancellation:** if you'd like to cancel/abort a watcher and close the change-stream permanently at a future time, pass a cancellation token to this parameter.

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
this event is fired when desired change events have been received from mongodb. for the above example, when author entities have been either created, updated or deleted, this event/action will receive those entities in batches. you can access the received entities via the input action parameter called `authors`.

if you'd like to receive the complete `ChangeStreamDocuments` instead of entities, you can subscribe to the `OnChangesCSD` method like so:
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
in case the change-stream ends due to an error, the `OnError` event will be fired with the exception. you can try to restart the watcher as shown above.

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
this event will be fired when the internal cursor gets closed due to either you requesting cancellation or an `invalidate` event occurring such as renaming or dropping of the watched collection.

if the cause of stopping is due to aborting via cancellation-token, the watcher has already purged all the event subscribers and no longer can be restarted.

if the cause was an `invalidate` event, you can restart watching as shown above. the existing event subscribers will continue to receive change events.

## Limiting properties of returned entities
you can apply a projection in order to specify which properties of your entity type you'd like returned when the change events are triggered like so:
```csharp
watcher.Start(
    eventTypes: EventType.Created | EventType.Updated,
    projection: a => new Author
    {
        Name = a.Name,
        Address = a.Address
    });
```
with the above example, only the author name and address properties will have their values populated. the rest of the properties will be null.

> [!note]
> projections cannot be done if the types of change you're interested in includes deletions. because the entity data no longer exists in the database when a deletion occurs and mongodb only returns the entity ID with the change event.


## Resuming across app restarts
you can retrieve a resume token from the `ResumeToken` property of the watcher like so:
```csharp
var token = watcher.ResumeToken;
```
persist this token to a non-volatile medium and use it upon app startup to initiate a watcher to continue/resume from where the app left off last time it was running.
```csharp
watcher.StartWithToken(token, ...);
```

## Access all watchers in the registry
```csharp
var watchers = DB.Watchers<Author>();

foreach (var w in watchers)
{
    Console.WriteLine("watcher: " + w.Name);
}
```
> [!note]
> there's a watcher registry per entity type and the watcher names need only be unique to each registry.

## Notes on resource usage
each watcher/change-stream you create opens a long-running cursor on the database server, which also means a persistent network connection between your application and the database. if you create more than a handful of watchers in your application, you should look in to increasing the size of the mongodb driver thread-pool size as shown below:

```csharp
await DB.InitAsync("DatabaseName", new MongoClientSettings()
{
    ...
    MaxConnectionPoolSize = 100 + NumberOfWatchers,
    ...
});
```

in addition to persistent network connections/cursors, each watcher will use a small amount of memory for an async/await state machine that does the actual work of iterating the change-stream cursor and emitting events without blocking threads during IO.

the bottom line is, change-streams can be a double-edged sword if not used sparingly. the beefier the machine that runs your app, the more change-streams you can create without affecting the performance of the rest of your application.