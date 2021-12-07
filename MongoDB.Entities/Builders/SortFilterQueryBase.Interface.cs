namespace MongoDB.Entities;

public interface ISortBuilder<T, TSelf>
{
    internal List<SortDefinition<T>> Sorts { get; }

    /// <summary>
    /// Specify which property and order to use for sorting (use multiple times if needed)
    /// </summary>
    /// <param name="propertyToSortBy">x => x.Prop</param>
    /// <param name="sortOrder">The sort order</param>
    public TSelf Sort(Expression<Func<T, object>> propertyToSortBy, Order sortOrder);

    /// <summary>
    /// Specify how to sort using a sort expression
    /// </summary>
    /// <param name="sortFunction">s => s.Ascending("Prop1").MetaTextScore("Prop2")</param>
    /// <returns></returns>
    public TSelf Sort(Func<SortDefinitionBuilder<T>, SortDefinition<T>> sortFunction);
}
