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
        public static Find<T> Find<T>(string? collectionName = null, IMongoCollection<T>? collection = null) where T : IEntity
            => new(Context, collection ?? Collection<T>(collectionName));

        /// <summary>
        /// Represents a MongoDB Find command
        /// <para>TIP: Specify your criteria using .Match() .Sort() .Skip() .Take() .Project() .Option() methods and finally call .Execute()</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TProjection">The type that is returned by projection</typeparam>
        public static Find<T, TProjection> Find<T, TProjection>(string? collectionName = null, IMongoCollection<T>? collection = null) where T : IEntity
            => new(Context, collection ?? Collection<T>(collectionName));
    }
}
