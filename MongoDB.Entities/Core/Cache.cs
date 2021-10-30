using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MongoDB.Entities
{
    internal abstract class Cache
    {
        //key: entity type
        //val: collection name
        protected static readonly ConcurrentDictionary<Type, string> typeToCollectionMap = new();

        internal static string CollectionNameFor(Type entityType)
            => typeToCollectionMap[entityType];
    }

    internal class Cache<T> : Cache where T : IEntity
    {
        internal static ConcurrentDictionary<string, Watcher<T>> Watchers { get; } = new();
        internal static bool HasCreatedOn { get; private set; }
        internal static bool HasModifiedOn { get; private set; }
        internal static string ModifiedOnPropName { get; private set; }
        internal static PropertyInfo ModifiedByProp { get; private set; }
        internal static bool HasIgnoreIfDefaultProps { get; private set; }
        internal static string CollectionName { get; set; }

        //key: TenantPrefix_CollectionName
        //val: IMongoCollection<T>
        private static readonly ConcurrentDictionary<string, IMongoCollection<T>> cache = new();
        private static string dbNameWithoutTenantPrefix;
        private static PropertyInfo[] updatableProps;
        private static ProjectionDefinition<T> requiredPropsProjection;

        static Cache()
        {
            Initialize();
            DB.DefaultDbChanged += Initialize;
        }

        private static void Initialize()
        {
            var type = typeof(T);
            var interfaces = type.GetInterfaces();

            var collAttrb = type.GetCustomAttribute<CollectionAttribute>(false) ??
#pragma warning disable CS0618 // Type or member is obsolete
                            type.GetCustomAttribute<NameAttribute>(false);
#pragma warning restore CS0618 // Type or member is obsolete

            CollectionName = collAttrb != null ? collAttrb.Name : type.Name;

            if (string.IsNullOrWhiteSpace(CollectionName) || CollectionName.Contains("~"))
                throw new ArgumentException($"{CollectionName} is an illegal name for a collection!");

            typeToCollectionMap[type] = CollectionName;

            SetDbNameWithoutTenantPrefix(DB.Database(null).DatabaseNamespace.DatabaseName); //default db for this type, which is overriden by calling DB.DatabaseFor<T>()

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

        internal static IMongoCollection<T> Collection(string tenantPrefix)
        {
            return cache.GetOrAdd($"{tenantPrefix}_{CollectionName}", _ =>
            {
                var dbName =
                    string.IsNullOrEmpty(tenantPrefix)
                    ? dbNameWithoutTenantPrefix
                    : $"{tenantPrefix}_{dbNameWithoutTenantPrefix}";

                return DB.Database(dbName).GetCollection<T>(CollectionName);
            });
        }

        internal static void SetDbNameWithoutTenantPrefix(string dbNameWithTenantPrefix)
            => dbNameWithoutTenantPrefix = dbNameWithTenantPrefix.Substring(dbNameWithTenantPrefix.IndexOf('_'));

        internal static IEnumerable<PropertyInfo> UpdatableProps(T entity)
        {
            if (HasIgnoreIfDefaultProps)
            {
                return updatableProps.Where(p =>
                    !(p.IsDefined(typeof(BsonIgnoreIfDefaultAttribute), false) && p.GetValue(entity) == default) &&
                    !(p.IsDefined(typeof(BsonIgnoreIfNullAttribute), false) && p.GetValue(entity) == null));
            }
            return updatableProps;
        }

        internal static ProjectionDefinition<T, TProjection> CombineWithRequiredProps<TProjection>(ProjectionDefinition<T, TProjection> userProjection)
        {
            if (userProjection == null)
                throw new InvalidOperationException("Please use .Project() method before .IncludeRequiredProps()");

            if (requiredPropsProjection is null)
            {
                requiredPropsProjection = "{_id:1}";

                var props = typeof(T)
                    .GetProperties()
                    .Where(p => p.IsDefined(typeof(BsonRequiredAttribute), false));

                if (!props.Any())
                    throw new InvalidOperationException("Unable to find any entity properties marked with [BsonRequired] attribute!");

                FieldAttribute attr;
                foreach (var p in props)
                {
                    attr = p.GetCustomAttribute<FieldAttribute>();

                    if (attr is null)
                        requiredPropsProjection = requiredPropsProjection.Include(p.Name);
                    else
                        requiredPropsProjection = requiredPropsProjection.Include(attr.ElementName);
                }
            }

            ProjectionDefinition<T> userProj = userProjection.Render(
                BsonSerializer.LookupSerializer<T>(),
                BsonSerializer.SerializerRegistry).Document;

            return Builders<T>.Projection.Combine(new[]
            {
                requiredPropsProjection,
                userProj
            });
        }
    }
}