using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;

namespace MongoDB.Entities;

internal class Cache<T> where T : IEntity
{
    private static readonly IDictionary<string, Cache<T>> TCache = new Dictionary<string, Cache<T>>();
    
    internal string DBName { get; private set; } = null!;
    internal IMongoDatabase Database { get; private set; } = null!;

    internal IMongoCollection<T> Collection { get; private set; } = null!;
    internal string CollectionName { get; private set; } = null!;

    internal ConcurrentDictionary<string, Watcher<T>> Watchers { get; private set; } = null!;

    internal bool HasCreatedOn { get; private set; }

    internal bool HasModifiedOn { get; private set; }
    internal string ModifiedOnPropName { get; private set; } = null!;
    internal PropertyInfo? ModifiedByProp { get; private set; }

    internal bool HasIgnoreIfDefaultProps { get; private set; }

    internal PropertyInfo IdProp { get; private set; } = null!;
    internal string IdPropName { get; private set; } = null!;
    internal Expression<Func<T, object?>> IdExpression { get; private set; } = null!;
    internal Func<T, object?> IdSelector { get; private set; } = null!;
    internal Action<object, object?> IdSetter { get; private set; } = null!;
    internal Func<object, object?> IdGetter { get; private set; } = null!;

    private PropertyInfo[] updatableProps = null!;

    private ProjectionDefinition<T> requiredPropsProjection = null!;

    // static Cache()
    // {
    //     Initialize();
    //     DB.DefaultDbChanged += Initialize;
    // }

    private Cache(Type type)
    {
        Initialize(type);
    }

    /// <summary>
    /// Gets the Cache object for a given IEntity instance.
    /// </summary>
    /// <param name="entity">Any class that implements IEntity</param>
    internal static Cache<T> Get(T entity)
    {
        return Get(entity.GetType());
    }

    internal static Cache<T> Get(Type type)
    {
        try
        {
            return TCache[type.FullName];
        }
        catch (KeyNotFoundException)
        {
            if (typeof(IEntity).IsAssignableFrom(type))
            {
                TCache[type.FullName] = new Cache<T>(type);
                return TCache[type.FullName];
            }
            throw new InvalidOperationException($"Type {type.FullName} must be an IEntity and specify an Identity property. '_id', 'Id', 'ID', or [BsonId] annotation expected!");
        }
    }
    
    private void Initialize(Type type)
    {
        var propertyInfo = type.GetIdPropertyInfo();
        if (propertyInfo != null)
        {
            IdProp = propertyInfo;
            IdPropName = propertyInfo.Name;
            IdExpression = SelectIdExpression(propertyInfo);
            IdSelector = IdExpression.Compile();
            IdGetter = type.GetterForProp(IdPropName);
            IdSetter = type.SetterForProp(IdPropName);
        }
        else
        {
            throw new InvalidOperationException($"Type {type.FullName} must specify an Identity property. '_id', 'Id', 'ID', or [BsonId] annotation expected!");
        }

        Database = TypeMap.GetDatabase(type);
        DBName = Database.DatabaseNamespace.DatabaseName;

        var collAttrb = type.GetCustomAttribute<CollectionAttribute>(false);

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

    internal IEnumerable<PropertyInfo> UpdatableProps(T entity)
    {
        return HasIgnoreIfDefaultProps
            ? updatableProps.Where(p =>
                !(p.IsDefined(typeof(BsonIgnoreIfDefaultAttribute), false) && p.GetValue(entity) == default) &&
                !(p.IsDefined(typeof(BsonIgnoreIfNullAttribute), false) && p.GetValue(entity) == null))
            : updatableProps;
    }

    internal ProjectionDefinition<T, TProjection> CombineWithRequiredProps<TProjection>(ProjectionDefinition<T, TProjection> userProjection)
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

                requiredPropsProjection = attr is null ? requiredPropsProjection.Include(p.Name) : requiredPropsProjection.Include(attr.ElementName);
            }
        }

        ProjectionDefinition<T> userProj = userProjection.Render(
            BsonSerializer.LookupSerializer<T>(),
            BsonSerializer.SerializerRegistry,
            LinqProvider.V3).Document;

        return Builders<T>.Projection.Combine(new[]
        {
            requiredPropsProjection,
            userProj
        });
    }

    private static Expression<Func<T, object?>> SelectIdExpression(PropertyInfo idProp)
    {
        var parameter = Expression.Parameter(typeof(T), "t");
        var property = Expression.Property(parameter, idProp);
        Expression conversion = Expression.Convert(property, typeof(object));
        return Expression.Lambda<Func<T, object?>>(conversion, parameter);
    }

}