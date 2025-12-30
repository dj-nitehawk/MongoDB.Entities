using System;
using MongoDB.Driver;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <summary>
    /// Adds a distinct aggregation stage to a fluent pipeline.
    /// </summary>
    /// <param name="aggregate"></param>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public static IAggregateFluent<T> Distinct<T>(this IAggregateFluent<T> aggregate) where T : IEntity
    {
        PipelineStageDefinition<T, T> groupStage =
            """
               {
                   $group: {
                       _id: '$_id',
                       doc: {
                           $first: '$$ROOT'
                       }
                   }
               }
            """;

        PipelineStageDefinition<T, T> rootStage =
            """
              {
                  $replaceRoot: {
                      newRoot: '$doc'
                  }
              }
            """;

        return aggregate.AppendStage(groupStage).AppendStage(rootStage);
    }

    /// <summary>
    /// Appends a match stage to the pipeline with a filter expression
    /// </summary>
    /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
    /// <param name="aggregate"></param>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public static IAggregateFluent<T> Match<T>(this IAggregateFluent<T> aggregate, Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter) where T : IEntity
        => aggregate.Match(filter(Builders<T>.Filter));

    /// <summary>
    /// Appends a match stage to the pipeline with an aggregation expression (i.e. $expr)
    /// </summary>
    /// <param name="expression">{ $gt: ['$Property1', '$Property2'] }</param>
    /// <param name="aggregate"></param>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public static IAggregateFluent<T> MatchExpression<T>(this IAggregateFluent<T> aggregate, string expression) where T : IEntity
    {
        PipelineStageDefinition<T, T> stage = "{$match:{$expr:" + expression + "}}";

        return aggregate.AppendStage(stage);
    }
}