using System.Linq;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DBContext
{
    /// <summary>
    /// Exposes the MongoDB collection for the given entity type as IQueryable in order to facilitate LINQ queries
    /// </summary>
    /// <param name="options">The aggregate options</param>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <param name="ignoreGlobalFilters">Set to true if you'd like to ignore any global filters for this operation</param>
    public IQueryable<T> Queryable<T>(AggregateOptions? options = null, bool ignoreGlobalFilters = false) where T : IEntity
    {
        var globalFilter = Logic.MergeWithGlobalFilter(ignoreGlobalFilters, _globalFilters, Builders<T>.Filter.Empty);

        return globalFilter != Builders<T>.Filter.Empty
                   ? _db.Queryable<T>(options, Session).Where(_ => globalFilter.Inject())
                   : _db.Queryable<T>(options, Session);
    }
}