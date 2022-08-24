using MongoDB.Driver;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <summary>
    /// Starts a paged search pipeline for this fluent pipeline
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <typeparam name="TProjection">The type of the resulting projection</typeparam>
    public static PagedSearch<T, TProjection> PagedSearch<T, TProjection>(this IAggregateFluent<T> aggregate) where T : IEntity
    {
        return DB.PagedSearch<T, TProjection>().WithFluent(aggregate);
    }

    /// <summary>
    /// Starts a paged search pipeline for this fluent pipeline
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public static PagedSearch<T, T> PagedSearch<T>(this IAggregateFluent<T> aggregate) where T : IEntity
    {
        return DB.PagedSearch<T, T>().WithFluent(aggregate);
    }
}
