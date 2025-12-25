using MongoDB.Bson;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <typeparam name="T">Any class that implements a MongoDB id </typeparam>
    extension<T>(T entity) where T : IEntity
    {
        /// <summary>
        /// Gets the name of the Identity object
        /// </summary>
        internal string GetIdName()
            => Cache<T>.IdPropName;

        /// <summary>
        /// Gets the Identity object
        /// </summary>
        internal object GetId()
            => Cache<T>.IdGetter(entity);

        /// <summary>
        /// Gets stored representation of the Identity object
        /// </summary>
        internal BsonValue GetBsonId()
        {
            var bsonEntity = entity.ToBsonDocument();

            return bsonEntity.GetValue(Cache<T>.IdBsonName);
        }

        /// <summary>
        /// Sets the Identity object
        /// </summary>
        internal void SetId(object id)
            => Cache<T>.IdSetter(entity, id);

        internal bool HasDefaultID()
            => Equals(Cache<T>.IdGetter(entity), Cache<T>.IdDefaultValue);
    }
}