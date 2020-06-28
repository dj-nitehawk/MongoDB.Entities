using MongoDB.Driver;
using MongoDB.Entities.Core;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MongoDB.Entities
{
    public partial class DB
    {
        private static readonly Dictionary<Type, string> entityColls = new Dictionary<Type, string>();

        internal static string GetCollectionName(Type type)
        {
            if (!entityColls.TryGetValue(type, out string coll))
            {
                var attribute = type.GetCustomAttribute<NameAttribute>(false);
                if (attribute != null)
                {
                    coll = attribute.Name;
                    entityColls[type] = coll;
                }
                else
                {
                    coll = type.Name;
                    entityColls[type] = coll;
                }
            }

            if (string.IsNullOrWhiteSpace(coll) || coll.Contains("~"))
                throw new ArgumentException("This is an illegal name for a collection!");

            return coll;
        }

        internal static IMongoCollection<JoinRecord> GetRefCollection<T>(string name) where T:IEntity
        {
            return GetDatabase<T>().GetCollection<JoinRecord>(name);
        }

        /// <summary>
        /// Gets the IMongoCollection for a given IEntity type.
        /// <para>TIP: Try never to use this unless really neccessary.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static IMongoCollection<T> Collection<T>() where T : IEntity,new()
        {
            return GetDatabase<T>().GetCollection<T>(CollectionName<T>());
        }

        /// <summary>
        /// Gets the IMongoCollection for a given IEntity type.
        /// <para>TIP: Try never to use this unless really neccessary.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public IMongoCollection<T> Collection<T>(string db = null) where T : IEntity,new()
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
