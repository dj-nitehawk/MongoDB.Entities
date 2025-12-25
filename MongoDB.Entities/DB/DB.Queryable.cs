using System.Linq;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB
{
    /// <summary>
    /// Exposes the MongoDB collection for the given entity type as IQueryable in order to facilitate LINQ queries
    /// </summary>
    /// <param name="options">The aggregate options</param>
    /// <typeparam name="T">The type of entity</typeparam>
    public IQueryable<T> Queryable<T>(AggregateOptions? options = null) where T : IEntity
    {
        var globalFilter = Logic.MergeWithGlobalFilter(IgnoreGlobalFilters, _globalFilters, Builders<T>.Filter.Empty);

        var queryable = SessionHandle == null
                            ? Collection<T>().AsQueryable(options)
                            : Collection<T>().AsQueryable(SessionHandle, options);

        return globalFilter != Builders<T>.Filter.Empty
                   ? queryable.Where(_ => globalFilter.Inject())
                   : queryable;
    }
}