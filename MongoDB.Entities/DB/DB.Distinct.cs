using MongoDB.Driver;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        /// <summary>
        /// Represents a MongoDB Distinct command where you can get back distinct values for a given property of a given Entity.
        /// </summary>
        /// <typeparam name="T">Any Entity that implements IEntity interface</typeparam>
        /// <typeparam name="TProperty">The type of the property of the entity you'd like to get unique values for</typeparam>
        /// <param name="collectionName">Specifiy to override the collection name</param>
        public static Distinct<T, TProperty> Distinct<T, TProperty>(string? collectionName = null) where T : IEntity
            => new(Context, Collection<T>(collectionName));
    }
}
