using MongoDB.Driver;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        /// <summary>
        /// Represents a MongoDB Find command
        /// <para>TIP: Specify your criteria using .Match() .Sort() .Skip() .Take() .Project() .Option() methods and finally call .Execute()</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="session">An optional session if using within a transaction</param>
        public static Find<T> Find<T>(IClientSessionHandle session = null) where T : IEntity
            => new Find<T>(session, null);

        /// <summary>
        /// Represents a MongoDB Find command
        /// <para>TIP: Specify your criteria using .Match() .Sort() .Skip() .Take() .Project() .Option() methods and finally call .Execute()</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TProjection">The type that is returned by projection</typeparam>
        /// <param name="session">An optional session if using within a transaction</param>
        public static Find<T, TProjection> Find<T, TProjection>(IClientSessionHandle session = null) where T : IEntity
            => new Find<T, TProjection>(session, null);
    }
}
