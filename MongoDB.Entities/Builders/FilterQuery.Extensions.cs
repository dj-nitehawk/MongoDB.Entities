namespace MongoDB.Entities;

public static class FilterExt
{
    //protected FilterDefinition<T> MergedFilter => Logic.MergeWithGlobalFilter(_ignoreGlobalFilters, _globalFilters, _filter);
    /// <summary>
    /// Specify an IEntity ID as the matching criteria
    /// </summary>
    /// <param name="self">the query</param>
    internal static FilterDefinition<TEntity> MergedFilter<TEntity, TSelf>(this TSelf self)
        where TSelf : IFilterBuilder<TEntity, TSelf>
    {
        return Logic.MergeWithGlobalFilter(self.IsIgnoreGlobalFilters, self.GlobalFilters, self.Filter);
    }

    /// <summary>
    /// Specify an IEntity ID as the matching criteria
    /// </summary>
    /// <param name="self">the query</param>
    /// <param name="id">A unique IEntity ID</param>
    public static TSelf MatchID<TEntity, TId, TSelf>(this TSelf self, TId id)
        where TId : IComparable<TId>, IEquatable<TId>
        where TEntity : IEntity<TId>
        where TSelf : IFilterBuilder<TEntity, TSelf>
    {
        return self.Match(f => f.Eq(t => t.ID, id));
    }

    /// <summary>
    /// Specify an IEntity ID as the matching criteria
    /// </summary>
    /// <param name="self">the query</param>
    /// <param name="id">A unique IEntity ID</param>
    public static TSelf Match<TEntity, TId, TSelf>(this TSelf self, TId id)
        where TId : IComparable<TId>, IEquatable<TId>
        where TEntity : IEntity<TId>
        where TSelf : IFilterBuilder<TEntity, TSelf>
    {
        return self.MatchID<TEntity, TId, TSelf>(id);
    }
}