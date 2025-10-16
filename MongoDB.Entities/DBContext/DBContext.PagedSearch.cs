namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DBContext
{
    /// <summary>
    /// Represents an aggregation query that retrieves results with easy paging support.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public PagedSearch<T> PagedSearch<T>() where T : IEntity
        => new(Session, _globalFilters, _db);

    /// <summary>
    /// Represents an aggregation query that retrieves results with easy paging support.
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <typeparam name="TProjection">The type you'd like to project the results to.</typeparam>
    public PagedSearch<T, TProjection> PagedSearch<T, TProjection>() where T : IEntity
        => new(Session, _globalFilters, _db);
}