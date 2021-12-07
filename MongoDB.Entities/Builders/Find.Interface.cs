
namespace MongoDB.Entities
{
    public interface IProjectionBuilder<T, TProjection, TSelf>
    {
        ///// <summary>
        ///// Specify to automatically include all properties marked with [BsonRequired] attribute on the entity in the final projection.
        ///// <para>HINT: this method should only be called after the .Project() method.</para>
        ///// </summary>
        //TSelf IncludeRequiredProps();

        /// <summary>
        /// Specify how to project the results using a lambda expression
        /// </summary>
        /// <param name="expression">x => new Test { PropName = x.Prop }</param>
        TSelf Project(Expression<Func<T, TProjection>> expression);

        /// <summary>
        /// Specify how to project the results using a projection expression
        /// </summary>
        /// <param name="projection">p => p.Include("Prop1").Exclude("Prop2")</param>
        TSelf Project(Func<ProjectionDefinitionBuilder<T>, ProjectionDefinition<T, TProjection>> projection);

        /// <summary>
        /// Specify how to project the results using an exclusion projection expression.
        /// </summary>
        /// <param name="exclusion">x => new { x.PropToExclude, x.AnotherPropToExclude }</param>
        TSelf ProjectExcluding(Expression<Func<T, object>> exclusion);


    }
    public interface IFindBuilder<T, TProjection, TSelf>
        where TSelf : IFindBuilder<T, TProjection, TSelf>
    {


        /// <summary>
        /// Specify how many entities to Take/Limit
        /// </summary>
        /// <param name="takeCount">The number to limit/take</param>
        TSelf Limit(int takeCount);

        /// <summary>
        /// Specify an option for this find command (use multiple times if needed)
        /// </summary>
        /// <param name="option">x => x.OptionName = OptionValue</param>
        TSelf Option(Action<FindOptions<T, TProjection>> option);

        /// <summary>
        /// Specify how many entities to skip
        /// </summary>
        /// <param name="skipCount">The number to skip</param>
        TSelf Skip(int skipCount);

        /// <summary>
        /// Sort the results of a text search by the MetaTextScore
        /// <para>TIP: Use this method after .Project() if you need to do a projection also</para>
        /// </summary>
        TSelf SortByTextScore();

        /// <summary>
        /// Sort the results of a text search by the MetaTextScore and get back the score as well
        /// <para>TIP: Use this method after .Project() if you need to do a projection also</para>
        /// </summary>
        /// <param name="scoreProperty">x => x.TextScoreProp</param>
        TSelf SortByTextScore<TProp>(Expression<Func<T, TProp>>? scoreProperty);
    }
}