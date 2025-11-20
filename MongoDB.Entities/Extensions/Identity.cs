using MongoDB.Bson;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <typeparam name="T">Any class that implements a MongoDB id </typeparam>
    extension<T>(T _) where T : IEntity
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
            => Cache<T>.IdGetter(_);

        /// <summary>
        /// Gets stored representation of the Identity object
        /// </summary>
        internal BsonValue GetBsonId()
        {
            var bsonEntity = _.ToBsonDocument();

            return bsonEntity.GetValue(Cache<T>.IdBsonName);
        }

        /// <summary>
        /// Sets the Identity object
        /// </summary>
        internal void SetId(object id)
            => Cache<T>.IdSetter(_, id);

        ///
        internal bool HasDefaultID()
            => Equals(Cache<T>.IdGetter(_), Cache<T>.IdDefaultValue);
    }

    // /// <summary>
    // /// When saving entities, this method will be called in order to determine if <see cref="GenerateNewID" /> needs to be called.
    // /// If this method returns <c>'true'</c>, <see cref="GenerateNewID" /> method is called and the ID (primary key) of the entity is populated.
    // /// If <c>'false'</c> is returned, it is assumed that ID generation is not required and the entity already has a non-default ID value.
    // /// </summary>
}