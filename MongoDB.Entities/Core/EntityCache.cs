using System.Collections.Concurrent;

namespace MongoDB.Entities;

internal class Cache
{
    protected PropertyInfo[] _updatableProps = null!;

    public bool HasCreatedOn { get; protected set; }
    public bool HasModifiedOn { get; protected set; }
    public string ModifiedOnPropName { get; } = nameof(IModifiedOn.ModifiedOn);
    public PropertyInfo? ModifiedByProp { get; protected set; }
    public bool HasIgnoreIfDefaultProps { get; protected set; }
    public string CollectionName { get; protected set; } = null!;
    public bool IsFileEntity { get; protected set; }
    protected Cache(Type type)
    {
        var interfaces = type.GetInterfaces();

        var collAttrb = type.GetCustomAttribute<CollectionAttribute>(false);

        CollectionName = collAttrb != null ? collAttrb.Name : type.Name;

        if (string.IsNullOrWhiteSpace(CollectionName) || CollectionName.Contains("~"))
            throw new ArgumentException($"{CollectionName} is an illegal name for a collection!");


        HasCreatedOn = interfaces.Any(i => i == typeof(ICreatedOn));
        HasModifiedOn = interfaces.Any(i => i == typeof(IModifiedOn));
        IsFileEntity = typeof(FileEntity).IsAssignableFrom(type.BaseType);

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
            ModifiedByProp = _updatableProps.SingleOrDefault(p => typeof(ModifiedBy).IsAssignableFrom(p.PropertyType));
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException("Multiple [ModifiedBy] properties are not allowed on entities!");
        }
    }




}

internal class EntityCache<T> : Cache
{
    public ConcurrentDictionary<string, Watcher<T>> Watchers { get; } = new();

    private static EntityCache<T>? _default;
    public static EntityCache<T> Default => _default ??= new();

    private EntityCache() : base(typeof(T))
    {
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

    private ProjectionDefinition<T>? _requiredPropsProjection;

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
