using MongoDB.Driver;
using MongoDB.Entities.Core;
using System;
using System.Reflection;

namespace MongoDB.Entities
{
    public partial class DB
    {
        internal static string GetCollectionName(Type type)
        {
            var attribute = type.GetCustomAttribute<NameAttribute>(false);
            var coll = attribute != null ? attribute.Name : type.Name;

            if (string.IsNullOrWhiteSpace(coll) || coll.Contains("~"))
                throw new ArgumentException("This is an illegal name for a collection!");

            return coll;
        }

        internal static IMongoCollection<JoinRecord> GetRefCollection<T>(string name) where T : IEntity
        {
            return GetDatabase<T>().GetCollection<JoinRecord>(name);
        }

        /// <summary>
        /// Gets the IMongoCollection for a given IEntity type.
        /// <para>TIP: Try never to use this unless really neccessary.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static IMongoCollection<T> Collection<T>() where T : IEntity
        {
            return GetDatabase<T>().GetCollection<T>(CollectionName<T>());
        }

        /// <summary>
        /// Gets the IMongoCollection for a given IEntity type.
        /// <para>TIP: Try never to use this unless really neccessary.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public IMongoCollection<T> Collection<T>(bool _ = false) where T : IEntity
        {
            return Collection<T>();
        }

        /// <summary>
        /// Gets the collection name for a given entity type
        /// </summary>
        /// <typeparam name="T">The type of entity to get the collection name for</typeparam>
        public static string CollectionName<T>() where T : IEntity
        {
            return GetCollectionName(typeof(T));
        }

        //todo: drop collection methods
    }
}
