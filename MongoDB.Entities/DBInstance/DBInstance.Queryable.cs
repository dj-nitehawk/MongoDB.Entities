using System.Linq;
using MongoDB.Driver;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DBInstance
{
    /// <summary>
    /// Exposes the MongoDB collection for the given IEntity as an IQueryable in order to facilitate LINQ queries.
    /// </summary>
    /// <param name="options">The aggregate options</param>
    /// <param name="session">An optional session if used within a transaction</param>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public IQueryable<T> Queryable<T>(AggregateOptions? options = null, IClientSessionHandle? session = null) where T : IEntity
        => session == null
               ? Collection<T>().AsQueryable(options)
               : Collection<T>().AsQueryable(session, options);
}