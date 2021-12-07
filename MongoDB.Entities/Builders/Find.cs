namespace MongoDB.Entities;

/// <summary>
/// Represents a MongoDB Find command.
/// <para>TIP: Specify your criteria using .Match() .Sort() .Skip() .Take() .Project() .Option() methods and finally call .Execute()</para>
/// <para>Note: For building queries, use the DB.Fluent* interfaces</para>
/// </summary>
/// <typeparam name="T">Any class that implements IEntity</typeparam>
public class Find<T> : Find<T, T>
{
    internal Find(DBContext context, IMongoCollection<T> collection)
        : base(context, collection) { }

    internal Find(DBContext context, IMongoCollection<T> collection, FindBase<T, T, Find<T, T>> baseQuery)
        : base(context, collection, baseQuery) { }
}


/// <summary>
/// Represents a MongoDB Find command with the ability to project to a different result type.
/// <para>TIP: Specify your criteria using .Match() .Sort() .Skip() .Take() .Project() .Option() methods and finally call .Execute()</para>
/// </summary>
/// <typeparam name="T">Any class that implements IEntity</typeparam>
/// <typeparam name="TProjection">The type you'd like to project the results to.</typeparam>
public class Find<T, TProjection> :
    FindBase<T, TProjection, Find<T, TProjection>>, ICollectionRelated<T>
{

    /// <summary>
    /// copy constructor
    /// </summary>
    /// <param name="other"></param>
    /// <param name="context"></param>
    /// <param name="collection"></param>
    internal Find(DBContext context, IMongoCollection<T> collection, FindBase<T, TProjection, Find<T, TProjection>> other) : base(other)
    {
        Context = context;
        Collection = collection;
    }
    internal Find(DBContext context, IMongoCollection<T> collection) : base(context.GlobalFilters)
    {
        Context = context;
        Collection = collection;
    }

    public override DBContext Context { get; }
    public IMongoCollection<T> Collection { get; private set; }





    /// <summary>
    /// Find entities by supplying a lambda expression
    /// </summary>
    /// <param name="expression">x => x.Property == Value</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <returns>A list of Entities</returns>
    public Task<List<TProjection>> ManyAsync(Expression<Func<T, bool>> expression, CancellationToken cancellation = default)
    {
        Match(expression);
        return ExecuteAsync(cancellation);
    }

    /// <summary>
    /// Find entities by supplying a filter expression
    /// </summary>
    /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <returns>A list of Entities</returns>
    public Task<List<TProjection>> ManyAsync(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter, CancellationToken cancellation = default)
    {
        Match(filter);
        return ExecuteAsync(cancellation);
    }

    /// <summary>
    /// Run the Find command in MongoDB server and get a list of results
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task<List<TProjection>> ExecuteAsync(CancellationToken cancellation = default)
    {
        var list = new List<TProjection>();
        using (var cursor = await ExecuteCursorAsync(cancellation).ConfigureAwait(false))
        {
            while (await cursor.MoveNextAsync(cancellation).ConfigureAwait(false))
            {
                list.AddRange(cursor.Current);
            }
        }
        return list;
    }

    /// <summary>
    /// Run the Find command in MongoDB server and get a single result or the default value if not found.
    /// If more than one entity is found, it will throw an exception.
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task<TProjection> ExecuteSingleAsync(CancellationToken cancellation = default)
    {
        Limit(2);
        using var cursor = await ExecuteCursorAsync(cancellation).ConfigureAwait(false);
        await cursor.MoveNextAsync(cancellation).ConfigureAwait(false);
        return cursor.Current.SingleOrDefault();
    }

    /// <summary>
    /// Run the Find command in MongoDB server and get the first result or the default value if not found
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task<TProjection> ExecuteFirstAsync(CancellationToken cancellation = default)
    {
        Limit(1);
        using var cursor = await ExecuteCursorAsync(cancellation).ConfigureAwait(false);
        await cursor.MoveNextAsync(cancellation).ConfigureAwait(false);
        return cursor.Current.SingleOrDefault(); //because we're limiting to 1
    }



    /// <summary>
    /// Run the Find command in MongoDB server and get a cursor instead of materialized results
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<IAsyncCursor<TProjection>> ExecuteCursorAsync(CancellationToken cancellation = default)
    {
        if (_sorts.Count > 0)
            _options.Sort = Builders<T>.Sort.Combine(_sorts);

        var mergedFilter = Logic.MergeWithGlobalFilter(_ignoreGlobalFilters, _globalFilters, _filter);

        return this.Session() is not IClientSessionHandle session ?
            Collection.FindAsync(mergedFilter, _options, cancellation) :
            Collection.FindAsync(session, mergedFilter, _options, cancellation);
    }

    /// <summary>
    /// Run the Find command and get back a bool indicating whether any entities matched the query
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task<bool> ExecuteAnyAsync(CancellationToken cancellation = default)
    {
        if (Context.Cache<T>().IsEntity)
        {
            Project(b => b.Include(nameof(IEntity.ID)));
        }
        Limit(1);
        using var cursor = await ExecuteCursorAsync(cancellation).ConfigureAwait(false);
        await cursor.MoveNextAsync(cancellation).ConfigureAwait(false);
        return cursor.Current.Any();
    }


}

public static class FindExt
{
    /// <summary>
    /// Find a single IEntity by ID
    /// </summary>
    /// <param name="self"></param>
    /// <param name="ID">The unique ID of an IEntity</param>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <returns>A single entity or null if not found</returns>
    public static Task<TProjection> OneAsync<TEntity, TId, TProjection, TSelf>(this TSelf self, TId ID, CancellationToken cancellation = default)
        where TId : IComparable<TId>, IEquatable<TId>
        where TEntity : IEntity<TId>
        where TSelf : Find<TEntity, TProjection>, IFilterBuilder<TEntity, TSelf>
    {
        self.MatchID<TEntity, TId, TSelf>(ID);
        return self.ExecuteSingleAsync(cancellation);
    }
}
public enum Order
{
    Ascending,
    Descending
}

public enum Search
{
    Fuzzy,
    Full
}
