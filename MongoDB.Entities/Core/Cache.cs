using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MongoDB.Entities
{
    internal static class Cache<T> where T : IEntity
    {
        internal static IMongoDatabase Database { get; private set; }
        internal static IMongoCollection<T> Collection { get; private set; }
        internal static string DBName { get; private set; }
        internal static string CollectionName { get; private set; }
        internal static ConcurrentDictionary<string, Watcher<T>> Watchers { get; private set; }
        internal static bool HasCreatedOn { get; private set; }
        internal static bool HasModifiedOn { get; private set; }
        internal static string ModifiedOnPropName { get; private set; }
        internal static PropertyInfo ModifiedByProp { get; private set; }
        internal static bool HasIgnoreIfDefaultProps { get; private set; }

        private static PropertyInfo[] updatableProps;

        static Cache()
        {
            Initialize();
            DB.DefaultDbChanged += Initialize;
        }

        private static void Initialize()
        {
            var type = typeof(T);

            Database = TypeMap.GetDatabase(type);
            DBName = Database.DatabaseNamespace.DatabaseName;

            var collAttrb = type.GetCustomAttribute<CollectionAttribute>(false) ??
#pragma warning disable CS0618 // Type or member is obsolete
                type.GetCustomAttribute<NameAttribute>(false);
#pragma warning restore CS0618 // Type or member is obsolete

            CollectionName = collAttrb != null ? collAttrb.Name : type.Name;

            if (string.IsNullOrWhiteSpace(CollectionName) || CollectionName.Contains("~"))
                throw new ArgumentException($"{CollectionName} is an illegal name for a collection!");

            Collection = Database.GetCollection<T>(CollectionName);
            TypeMap.AddCollectionMapping(type, CollectionName);

            Watchers = new ConcurrentDictionary<string, Watcher<T>>();

            var interfaces = type.GetInterfaces();
            HasCreatedOn = interfaces.Any(i => i == typeof(ICreatedOn));
            HasModifiedOn = interfaces.Any(i => i == typeof(IModifiedOn));
            ModifiedOnPropName = nameof(IModifiedOn.ModifiedOn);

            updatableProps = type.GetProperties()
                .Where(p =>
                       p.PropertyType.Name != ManyBase.PropTypeName &&
                      !p.IsDefined(typeof(BsonIdAttribute), false) &&
                      !p.IsDefined(typeof(BsonIgnoreAttribute), false))
                .ToArray();

            HasIgnoreIfDefaultProps = updatableProps.Any(p =>
                    p.IsDefined(typeof(BsonIgnoreIfDefaultAttribute), false) ||
                    p.IsDefined(typeof(BsonIgnoreIfNullAttribute), false));

            try
            {
                ModifiedByProp = updatableProps.SingleOrDefault(p =>
                                p.PropertyType == typeof(ModifiedBy) ||
                                p.PropertyType.IsSubclassOf(typeof(ModifiedBy)));
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("Multiple [ModifiedBy] properties are not allowed on entities!");
            }
        }

        public static IEnumerable<PropertyInfo> UpdatableProps(T entity)
        {
            if (HasIgnoreIfDefaultProps)
            {
                return updatableProps.Where(p =>
                    !(p.IsDefined(typeof(BsonIgnoreIfDefaultAttribute), false) && p.GetValue(entity) == default) &&
                    !(p.IsDefined(typeof(BsonIgnoreIfNullAttribute), false) && p.GetValue(entity) == null));
            }
            return updatableProps;
        }
    }
}