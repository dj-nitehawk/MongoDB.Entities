# Async-only api
This library no longer supports synchronous operations after version 20 as it was discovered that the official mongodb driver is doing faux sync (sync-over-async anti-pattern) under the hood in order to maintain backward compatibility. 

> "One caveat is that the synchronous legacy API in 2.0 is implemented by calling the low level async API and blocking, waiting for the Task to complete. This is not considered a performant way to use async APIs, so for performance-sensitive code you may prefer to use the 1.10 version of the driver until you are ready to convert your application to use the new async API." - [_Robert Stem_](https://www.mongodb.com/blog/post/introducing-20-net-driver)

stress/load testing showed that it is inefficient at handling large volumes leading to thread-pool starvation. since the official driver has been made fully async after v2.0, it was decided to discourage consumers of this library from using the faux-sync api of the driver by removing all sync wrapper methods and only support async operations for IO bound work going forward.

it is highly recommended you build applications that run in server environments fully async from top to bottom in order to make sure they scale well.

however, in places where you can't call async code, you can wrap the async methods in a `Task.Run()` like so:

```csharp
Task.Run(async () =>
{
    await DB.InitAsync("MyDatabase", "127.0.0.1");
})
.GetAwaiter()
.GetResult();
```

> [!tip]
> try not to do that except for calling the init method once at app start-up. 

# LINQ async extensions

in order to write async LINQ queries, make sure to import the mongodb linq extensions and write queries as follows:

```csharp
using MongoDB.Driver;
using MongoDB.Driver.Linq;
```
```csharp
var lastAuthor = await (from a in author.Queryable()
                        orderby a.ModifiedOn descending
                        select a).FirstOrDefaultAsync();
```