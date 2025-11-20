using MongoDB.Driver;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <param name="aggregate"></param>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    extension<T>(IAggregateFluent<T> aggregate) where T : IEntity
    {
        /// <summary>
        /// Starts a paged search pipeline for this fluent pipeline
        /// </summary>
        /// <typeparam name="TProjection">The type of the resulting projection</typeparam>
        /// <param name="db">The DB instance to use for this operation</param>
        public PagedSearch<T, TProjection> PagedSearch<TProjection>(DB? db = null)
            => DB.InstanceOrDefault(db).PagedSearch<T, TProjection>().WithFluent(aggregate);

        /// <summary>
        /// Starts a paged search pipeline for this fluent pipeline
        /// </summary>
        /// <param name="db">The DB instance to use for this operation</param>
        public PagedSearch<T, T> PagedSearch(DB? db = null)
            => DB.InstanceOrDefault(db).PagedSearch<T, T>().WithFluent(aggregate);
    }
}