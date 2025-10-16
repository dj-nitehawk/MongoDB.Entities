using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace MongoDB.Entities;

static class Cache<T> where T : IEntity
{
    internal static string CollectionName { get; private set; } = null!;
    internal static ConcurrentDictionary<DBInstance, ConcurrentDictionary<string, Watcher<T>>> Watchers { get; private set; } = null!;
    internal static bool HasCreatedOn { get; private set; }
    internal static bool HasModifiedOn { get; private set; }
    internal static string ModifiedOnPropName { get; private set; } = null!;
    internal static PropertyInfo? ModifiedByProp { get; private set; }
    internal static bool HasIgnoreIfDefaultProps { get; private set; }
    internal static string IdPropName { get; private set; } = null!;
    internal static Expression<Func<T, object?>> IdExpression { get; private set; } = null!;
    internal static Func<T, object?> IdSelector { get; private set; } = null!;
    internal static Action<object, object> IdSetter { get; private set; } = null!;
    internal static Func<object, object> IdGetter { get; private set; } = null!;

    static PropertyInfo[] _updatableProps = [];
    static ProjectionDefinition<T>? _requiredPropsProjection;

    static Cache()
    {
        Initialize();
    }

    static void Initialize()
    {
        var type = typeof(T);

        var propertyInfo = type.GetIdPropertyInfo();

        if (propertyInfo != null)
        {
            IdPropName = propertyInfo.Name;
            IdExpression = SelectIdExpression(propertyInfo);
            IdSelector = IdExpression.Compile();
            IdGetter = type.GetterForProp(IdPropName);
            IdSetter = type.SetterForProp(IdPropName);
        }
        else
        {
            throw new InvalidOperationException(
                $"Type {type.FullName} must specify an Identity property. '_id', 'Id', 'ID', or [BsonId] annotation expected!");
        }

        var collAttrb = type.GetCustomAttribute<CollectionAttribute>(false);

        CollectionName = collAttrb != null ? collAttrb.Name : type.Name;

        if (string.IsNullOrWhiteSpace(CollectionName) || CollectionName.Contains("~"))
            throw new ArgumentException($"{CollectionName} is an illegal name for a collection!");
        
        Watchers = new();

        var interfaces = type.GetInterfaces();
        HasCreatedOn = interfaces.Any(i => i == typeof(ICreatedOn));
        HasModifiedOn = interfaces.Any(i => i == typeof(IModifiedOn));
        ModifiedOnPropName = nameof(IModifiedOn.ModifiedOn);

        _updatableProps = type.GetProperties()
                              .Where(
                                  p =>
                                      p.PropertyType.Name != ManyBase.PropTypeName &&
                                      !p.IsDefined(typeof(BsonIdAttribute), false) &&
                                      !p.IsDefined(typeof(BsonIgnoreAttribute), false))
                              .ToArray();

        HasIgnoreIfDefaultProps = _updatableProps.Any(
            p =>
                p.IsDefined(typeof(BsonIgnoreIfDefaultAttribute), false) ||
                p.IsDefined(typeof(BsonIgnoreIfNullAttribute), false));

        try
        {
            ModifiedByProp = _updatableProps.SingleOrDefault(
                p =>
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
                   ? _updatableProps.Where(
                       p =>
                           !(p.IsDefined(typeof(BsonIgnoreIfDefaultAttribute), false) && p.GetValue(entity) == default) &&
                           !(p.IsDefined(typeof(BsonIgnoreIfNullAttribute), false) && p.GetValue(entity) == null))
                   : _updatableProps;
    }

    internal static ProjectionDefinition<T, TProjection> CombineWithRequiredProps<TProjection>(ProjectionDefinition<T, TProjection> userProjection)
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

            foreach (var p in props)
            {
                var attr = p.GetCustomAttribute<FieldAttribute>();
                _requiredPropsProjection = attr is null ? _requiredPropsProjection.Include(p.Name) : _requiredPropsProjection.Include(attr.ElementName);
            }
        }

        ProjectionDefinition<T> userProj = userProjection.Render(
            new(BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry)).Document;

        return Builders<T>.Projection.Combine(_requiredPropsProjection, userProj);
    }

    static Expression<Func<T, object?>> SelectIdExpression(PropertyInfo idProp)
    {
        var parameter = Expression.Parameter(typeof(T), "t");
        var property = Expression.Property(parameter, idProp);
        Expression conversion = Expression.Convert(property, typeof(object));

        return Expression.Lambda<Func<T, object?>>(conversion, parameter);
    }
}