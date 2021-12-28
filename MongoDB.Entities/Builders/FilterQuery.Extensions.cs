namespace MongoDB.Entities;

//i dream to live in a world where c# can infer generic arguments on its own
public static class FilterExt
{

    /// <summary>
    /// Specify an IEntity ID as the matching criteria
    /// </summary>
    /// <param name="self">the query</param>
    /// <param name="id">A unique IEntity ID</param>
    public static TSelf MatchID<TEntity, TId, TSelf>(this IFilterBuilder<TEntity, TSelf> self, TId id)
        where TId : IComparable<TId>, IEquatable<TId>
        where TEntity : IEntity<TId>
        where TSelf : IFilterBuilder<TEntity, TSelf>
        => self.Match(f => f.Eq(t => t.ID, id));


    /// <summary>
    /// Specify an IEntity ID as the matching criteria
    /// </summary>
    /// <param name="self">the query</param>
    /// <param name="id">A unique IEntity ID</param>
    public static TSelf MatchID<TEntity, TSelf>(this IFilterBuilder<TEntity, TSelf> self, string id)
        where TEntity : IEntity
        where TSelf : IFilterBuilder<TEntity, TSelf>
    => MatchID<TEntity, string, TSelf>(self, id);





    /// <summary>
    /// Specify an IEntity ID as the matching criteria
    /// </summary>
    /// <param name="self">the query</param>
    /// <param name="id">A unique IEntity ID</param>
    public static TSelf Match<TEntity, TId, TSelf>(this IFilterBuilder<TEntity, TSelf> self, TId id)
        where TId : IComparable<TId>, IEquatable<TId>
        where TEntity : IEntity<TId>
        where TSelf : IFilterBuilder<TEntity, TSelf>
        => self.MatchID(id);


    /// <summary>
    /// Specify an IEntity ID as the matching criteria
    /// </summary>
    /// <param name="self">the query</param>
    /// <param name="id">A unique IEntity ID</param>
    public static TSelf Match<TEntity, TSelf>(this IFilterBuilder<TEntity, TSelf> self, string id)
        where TEntity : IEntity
        where TSelf : IFilterBuilder<TEntity, TSelf>
        => self.MatchID(id);
}