using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#nullable enable
namespace MongoDB.Entities
{
    internal abstract class Cache
    {
        //key: entity type
        //val: collection name
        protected readonly ConcurrentDictionary<Type, string> typeToCollectionNameMap = new();

        //key: entity type
        //val: database name without tenant prefix (will be null if not specifically set using DB.DatabaseFor<T>() method)
        protected readonly ConcurrentDictionary<Type, string> typeToDbNameWithoutTenantPrefixMap = new();

        internal string CollectionNameFor(Type entityType)
            => typeToCollectionNameMap[entityType];

        internal void MapTypeToDbNameWithoutTenantPrefix<T>(string dbNameWithoutTenantPrefix) where T : IEntity
            => typeToDbNameWithoutTenantPrefixMap[typeof(T)] = dbNameWithoutTenantPrefix;

        internal string GetFullDbName(Type entityType, string tenantPrefix)
        {
            string fullDbName = null;

            string dbNameWithoutTenantPrefix = typeToDbNameWithoutTenantPrefixMap[entityType];

            if (!string.IsNullOrEmpty(dbNameWithoutTenantPrefix))
            {
                if (!string.IsNullOrEmpty(tenantPrefix))
                    fullDbName = $"{tenantPrefix}~{dbNameWithoutTenantPrefix}";
                else
                    fullDbName = dbNameWithoutTenantPrefix;
            }

            return fullDbName;
        }
    }

    internal class Cache<T> : Cache where T : IEntity
    {
        private static Cache<T>? _instance;
        public static Cache<T> Instance => _instance ??= new();

        public ConcurrentDictionary<string, Watcher<T>> Watchers { get; } = new();
        public bool HasCreatedOn { get; }
        public bool HasModifiedOn { get; }
        public string ModifiedOnPropName { get; }
        public PropertyInfo ModifiedByProp { get; }
        public bool HasIgnoreIfDefaultProps { get; }
        public string CollectionName { get; }
        public bool IsFileEntity { get; }

        //key: TenantPrefix:CollectionName
        //val: IMongoCollection<T>
        private readonly ConcurrentDictionary<string, IMongoCollection<T>> _cache = new();
        private readonly PropertyInfo[] _updatableProps;
        private ProjectionDefinition<T>? _requiredPropsProjection;

        public Cache()
        {
            if (_instance == null) _instance = this;
            var type = typeof(T);
            var interfaces = type.GetInterfaces();

            var collAttrb = type.GetCustomAttribute<CollectionAttribute>(false);

            CollectionName = collAttrb != null ? collAttrb.Name : type.Name;

            if (string.IsNullOrWhiteSpace(CollectionName) || CollectionName.Contains("~"))
                throw new ArgumentException($"{CollectionName} is an illegal name for a collection!");

            typeToCollectionNameMap[type] = CollectionName;

            if (!typeToDbNameWithoutTenantPrefixMap.ContainsKey(type))
                typeToDbNameWithoutTenantPrefixMap[type] = null;

            HasCreatedOn = interfaces.Any(i => i == typeof(ICreatedOn));
            HasModifiedOn = interfaces.Any(i => i == typeof(IModifiedOn));
            ModifiedOnPropName = nameof(IModifiedOn.ModifiedOn);
            IsFileEntity = type.BaseType == typeof(FileEntity);

            _updatableProps = type.GetProperties()
                .Where(p =>
                       p.PropertyType.Name != ManyBase.PropTypeName &&
                      !p.IsDefined(typeof(BsonIdAttribute), false) &&
                      !p.IsDefined(typeof(BsonIgnoreAttribute), false))
                .ToArray();

            HasIgnoreIfDefaultProps = _updatableProps.Any(p =>
                    p.IsDefined(typeof(BsonIgnoreIfDefaultAttribute), false) ||
                    p.IsDefined(typeof(BsonIgnoreIfNullAttribute), false));

            try
            {
                ModifiedByProp = _updatableProps.SingleOrDefault(p =>
                                p.PropertyType == typeof(ModifiedBy) ||
                                p.PropertyType.IsSubclassOf(typeof(ModifiedBy)));
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("Multiple [ModifiedBy] properties are not allowed on entities!");
            }
        }

        public IMongoCollection<T> Collection(string tenantPrefix)
        {
            return _cache.GetOrAdd(
                key: $"{tenantPrefix}:{CollectionName}",
                valueFactory: _ => DB.Database(GetFullDbName(typeof(T), tenantPrefix)).GetCollection<T>(CollectionName));
        }

        public IEnumerable<PropertyInfo> UpdatableProps(T entity)
        {
            if (HasIgnoreIfDefaultProps)
            {
                return _updatableProps.Where(p =>
                    !(p.IsDefined(typeof(BsonIgnoreIfDefaultAttribute), false) && p.GetValue(entity) == default) &&
                    !(p.IsDefined(typeof(BsonIgnoreIfNullAttribute), false) && p.GetValue(entity) == null));
            }
            return _updatableProps;
        }

        public ProjectionDefinition<T, TProjection> CombineWithRequiredProps<TProjection>(ProjectionDefinition<T, TProjection> userProjection)
        {
            if (userProjection == null)
                throw new InvalidOperationException("Please use .Project() method before .IncludeRequiredProps()");

            if (_requiredPropsProjection is null)
            {
                _requiredPropsProjection = "{_id:1}";

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
                        _requiredPropsProjection = _requiredPropsProjection.Include(p.Name);
                    else
                        _requiredPropsProjection = _requiredPropsProjection.Include(attr.ElementName);
                }
            }

            ProjectionDefinition<T> userProj = userProjection.Render(
                BsonSerializer.LookupSerializer<T>(),
                BsonSerializer.SerializerRegistry).Document;

            return Builders<T>.Projection.Combine(new[]
            {
                _requiredPropsProjection,
                userProj
            });
        }
    }
}