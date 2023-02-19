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

namespace MongoDB.Entities;

internal static class Cache<T> where T : IEntity
{
    internal static string DBName { get; private set; } = null!;
    internal static IMongoDatabase Database { get; private set; } = null!;

    internal static IMongoCollection<T> Collection { get; private set; } = null!;
    internal static string CollectionName { get; private set; } = null!;

    internal static ConcurrentDictionary<string, Watcher<T>> Watchers { get; private set; } = null!;

    internal static bool HasCreatedOn { get; private set; }

    internal static bool HasModifiedOn { get; private set; }
    internal static string ModifiedOnPropName { get; private set; } = null!;
    internal static PropertyInfo? ModifiedByProp { get; private set; }

    internal static bool HasIgnoreIfDefaultProps { get; private set; }

    internal static PropertyInfo IdProp { get; private set; } = null!;
    internal static string IdPropName { get; private set; } = null!;
    internal static Expression<Func<T, object?>> IdExpression { get; private set; } = null!;
    internal static Func<T, object?> IdSelector { get; private set; } = null!;

    private static PropertyInfo[] updatableProps = null!;

    private static ProjectionDefinition<T> requiredPropsProjection = null!;

    static Cache()
    {
        Initialize();
        DB.DefaultDbChanged += Initialize;
    }

    private static void Initialize()
    {
        var type = typeof(T);

        var propertyInfo = type.GetIdPropertyInfo();
        if (propertyInfo != null)
        {
            IdProp = propertyInfo;
            IdPropName = propertyInfo.Name;
            IdExpression = SelectIdExpression(propertyInfo);
            IdSelector = IdExpression.Compile();
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

    internal static IEnumerable<PropertyInfo> UpdatableProps(T entity)
    {
        return HasIgnoreIfDefaultProps
            ? updatableProps.Where(p =>
                !(p.IsDefined(typeof(BsonIgnoreIfDefaultAttribute), false) && p.GetValue(entity) == default) &&
                !(p.IsDefined(typeof(BsonIgnoreIfNullAttribute), false) && p.GetValue(entity) == null))
            : updatableProps;
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
        return Expression.Lambda<Func<T, object?>>(property, parameter);
    }
}