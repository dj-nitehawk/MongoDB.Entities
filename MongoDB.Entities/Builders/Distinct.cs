namespace MongoDB.Entities;

/// <summary>
/// Represents a MongoDB Distinct command where you can get back distinct values for a given property of a given Entity.
/// </summary>
/// <typeparam name="T">Any Entity that implements IEntity interface</typeparam>
/// <typeparam name="TProperty">The type of the property of the entity you'd like to get unique values for</typeparam>
public class Distinct<T, TProperty> : DistinctBase<T, TProperty, Distinct<T, TProperty>>, ICollectionRelated<T>
{
    public DBContext Context { get; }
    public IMongoCollection<T> Collection { get; }

    internal Distinct(
        DBContext context,
        IMongoCollection<T> collection,
        DistinctBase<T, TProperty, Distinct<T, TProperty>> other) : base(other)
    {
        Context = context;
        Collection = collection;
    }
    internal Distinct(
        DBContext context,
        IMongoCollection<T> collection) : base(globalFilters: context.GlobalFilters)
    {
        Context = context;
        Collection = collection;
    }

    /// <summary>
    /// Run the Distinct command in MongoDB server and get a cursor instead of materialized results
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<IAsyncCursor<TProperty>> ExecuteCursorAsync(CancellationToken cancellation = default)
    {
        if (_field == null)
            throw new InvalidOperationException("Please use the .Property() method to specify the field to use for obtaining unique values for!");

        var mergedFilter = MergedFilter;

        return Context.Session is IClientSessionHandle session
               ? Collection.DistinctAsync(session, _field, mergedFilter, _options, cancellation)
               : Collection.DistinctAsync(_field, mergedFilter, _options, cancellation);
    }

    /// <summary>
    /// Run the Distinct command in MongoDB server and get a list of unique property values
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task<List<TProperty>> ExecuteAsync(CancellationToken cancellation = default)
    {
        var list = new List<TProperty>();
        using (var csr = await ExecuteCursorAsync(cancellation).ConfigureAwait(false))
        {
            while (await csr.MoveNextAsync(cancellation).ConfigureAwait(false))
            {
                list.AddRange(csr.Current);
            }
        }
        return list;
    }
}
