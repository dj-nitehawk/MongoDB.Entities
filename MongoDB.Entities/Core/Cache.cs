using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace MongoDB.Entities;

static class Cache<T> where T : IEntity
{
    internal static BsonClassMap BsonClassMap { get; private set; } = null!;
    internal static string CollectionName { get; private set; } = null!;
    internal static ConcurrentDictionary<DB, ConcurrentDictionary<string, Watcher<T>>> Watchers { get; private set; } = null!;
    internal static bool HasCreatedOn { get; private set; }
    internal static bool HasModifiedOn { get; private set; }
    internal static string ModifiedOnPropName { get; private set; } = null!;
    internal static PropertyInfo? ModifiedByProp { get; private set; }
    internal static bool HasIgnoreIfDefaultProps { get; private set; }
    internal static string IdPropName { get; private set; } = null!;
    internal static string IdBsonName { get; private set; } = null!;
    internal static Expression<Func<T, object?>> IdExpression { get; private set; } = null!;
    internal static Expression<Func<T, BsonValue>> BsonValueIdExpression { get; private set; } = null!;
    internal static Func<T, object?> IdSelector { get; private set; } = null!;
    internal static Action<object, object> IdSetter { get; private set; } = null!;
    internal static Func<object, object> IdGetter { get; private set; } = null!;
    internal static object IdDefaultValue { get; private set; } = null!;
    internal static IIdGenerator? IdGenerator { get; private set; }

    static PropertyInfo[] _updatableProps = [];
    static ProjectionDefinition<T>? _requiredPropsProjection;

    internal static ConcurrentDictionary<string, IMongoCollection<JoinRecord>> ReferenceCollections { get; } = new();

    static Cache()
    {
        Initialize();
    }

    static void Initialize()
    {
        var type = typeof(T);

        BsonClassMap = MapBsonClass(type);

        var idMap = BsonClassMap.IdMemberMap;

        if (idMap != null)
        {
            IdPropName = idMap.MemberName;
            IdBsonName = idMap.ElementName;
            IdExpression = SelectIdExpression(idMap.MemberInfo);
            BsonValueIdExpression = SelectBsonValueIdExpression(idMap.MemberInfo);
            IdSelector = IdExpression.Compile();
            IdGetter = idMap.Getter;
            IdSetter = idMap.Setter;
            IdDefaultValue = idMap.DefaultValue;
            IdGenerator = ResolveIdGenerator(idMap);
        }
        else
            throw new InvalidOperationException($"Type {type.FullName} must specify an Identity property. '_id', 'Id', 'ID', or [BsonId] annotation expected!");

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
                           !(p.IsDefined(typeof(BsonIgnoreIfDefaultAttribute), false) && p.GetValue(entity) == null) &&
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

        ProjectionDefinition<T> userProj =
            userProjection.Render(new(BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry)).Document;

        return Builders<T>.Projection.Combine(_requiredPropsProjection, userProj);
    }

    static Expression<Func<T, object?>> SelectIdExpression(MemberInfo idProp)
    {
        var parameter = Expression.Parameter(typeof(T), "t");
        var property = Expression.Property(parameter, idProp.Name);
        Expression conversion = Expression.Convert(property, typeof(object));

        return Expression.Lambda<Func<T, object?>>(conversion, parameter);
    }

    // used as a server-side join/lookup key against JoinRecord's BsonValue fields.
    // the double conversion never executes; the driver only translates the ID member path.
    static Expression<Func<T, BsonValue>> SelectBsonValueIdExpression(MemberInfo idProp)
    {
        var parameter = Expression.Parameter(typeof(T), "t");
        var property = Expression.Property(parameter, idProp.Name);
        Expression conversion = Expression.Convert(Expression.Convert(property, typeof(object)), typeof(BsonValue));

        return Expression.Lambda<Func<T, BsonValue>>(conversion, parameter);
    }

    /// <summary>
    /// Converts a CLR ID value to the representation it would be stored as in the database, by running it
    /// through the serializer of the ID property of the entity. i.e. a string decorated with an ObjectId
    /// representation attribute yields a BsonObjectId, a plain string yields a BsonString, etc.
    /// </summary>
    internal static BsonValue IdToBsonValue(object? id)
        => DB.ToBsonValue(BsonClassMap.IdMemberMap, id);

    // explicit registration for this entity type via DB.RegisterIdGenerator<T>() overrides the resolved generator.
    // works regardless of where the ID property is declared and regardless of registration order vs. first use.
    internal static void SetIdGenerator(IIdGenerator generator)
        => IdGenerator = generator;

    // the generator set on the class map (via SetIdGenerator or driver conventions) wins, then any
    // generator registered with BsonSerializer.RegisterIdGenerator for the ID's CLR type, then
    // library defaults matching the ID formats generated for the well-known ID types.
    static IIdGenerator? ResolveIdGenerator(BsonMemberMap idMap)
        => idMap.IdGenerator
           ?? BsonSerializer.LookupIdGenerator(idMap.MemberType)
           ?? (idMap.MemberType == typeof(string)
                   ? StringObjectIdGenerator.Instance
                   : idMap.MemberType == typeof(ObjectId)
                       ? ObjectIdGenerator.Instance
                       : idMap.MemberType == typeof(Guid)
                           ? GuidGenerator.Instance
                           : null);

    static BsonClassMap MapBsonClass(Type type)
    {
        if (type.BaseType != typeof(object) && !BsonClassMap.IsClassMapRegistered(type.BaseType!))
            MapBsonClass(type.BaseType!);

        if (!BsonClassMap.IsClassMapRegistered(type))
        {
            var cm = new BsonClassMap(type);
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);

            BsonClassMap.RegisterClassMap(cm);
        }

        return BsonClassMap.LookupClassMap(type);
    }

    internal static bool AddReferenceCollection(string name, IMongoCollection<JoinRecord> collection)
        => ReferenceCollections.TryAdd(name, collection);
}