namespace MongoDB.Entities;

/// <summary>
/// Represents an index creation command
/// <para>TIP: Define the keys first with .Key() method and finally call the .Create() method.</para>
/// </summary>
/// <typeparam name="T">Any class that implements IEntity</typeparam>
/// <typeparam name="TId">Id type</typeparam>
public class Index<T, TId> : ICollectionRelated<T>
            where TId : IComparable<TId>, IEquatable<TId>
    where T : IEntity<TId>
{
    internal List<Key<T, TId>> Keys { get; set; } = new();
    public DBContext Context { get; }
    public IMongoCollection<T> Collection { get; }

    private readonly CreateIndexOptions<T> _options = new() { Background = true };

    internal Index(DBContext context, IMongoCollection<T> collection)
    {
        Context = context;
        Collection = collection;
    }

    /// <summary>
    /// Call this method to finalize defining the index after setting the index keys and options.
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <returns>The name of the created index</returns>
    public async Task<string> CreateAsync(CancellationToken cancellation = default)
    {
        if (Keys.Count == 0) throw new ArgumentException("Please define keys before calling this method.");

        var propNames = new List<string>();
        var keyDefs = new List<IndexKeysDefinition<T>>();
        var isTextIndex = false;

        foreach (var key in Keys)
        {
            string keyType = string.Empty;

            switch (key.Type)
            {
                case KeyType.Ascending:
                    keyDefs.Add(Builders<T>.IndexKeys.Ascending(key.PropertyName));
                    keyType = "(Asc)";
                    break;
                case KeyType.Descending:
                    keyDefs.Add(Builders<T>.IndexKeys.Descending(key.PropertyName));
                    keyType = "(Dsc)";
                    break;
                case KeyType.Geo2D:
                    keyDefs.Add(Builders<T>.IndexKeys.Geo2D(key.PropertyName));
                    keyType = "(G2d)";
                    break;
                case KeyType.Geo2DSphere:
                    keyDefs.Add(Builders<T>.IndexKeys.Geo2DSphere(key.PropertyName));
                    keyType = "(Gsp)";
                    break;
                case KeyType.Hashed:
                    keyDefs.Add(Builders<T>.IndexKeys.Hashed(key.PropertyName));
                    keyType = "(Hsh)";
                    break;
                case KeyType.Text:
                    keyDefs.Add(Builders<T>.IndexKeys.Text(key.PropertyName));
                    isTextIndex = true;
                    break;
                case KeyType.Wildcard:
                    keyDefs.Add(Builders<T>.IndexKeys.Wildcard(key.PropertyName));
                    keyType = "(Wld)";
                    break;
            }
            propNames.Add(key.PropertyName + keyType);
        }

        if (string.IsNullOrEmpty(_options.Name))
        {
            if (isTextIndex)
                _options.Name = "[TEXT]";
            else
                _options.Name = string.Join(" | ", propNames);
        }

        var model = new CreateIndexModel<T>(
            Builders<T>.IndexKeys.Combine(keyDefs),
            _options);

        try
        {
            await CreateAsync(model, cancellation).ConfigureAwait(false);
        }
        catch (MongoCommandException x) when (x.Code == 85 || x.Code == 86)
        {
            await DropAsync(_options.Name, cancellation).ConfigureAwait(false);
            await CreateAsync(model, cancellation).ConfigureAwait(false);
        }

        return _options.Name;
    }

    /// <summary>
    /// Set the options for this index definition
    /// <para>TIP: Setting options is not required.</para>
    /// </summary>
    /// <param name="option">x => x.OptionName = OptionValue</param>
    public Index<T, TId> Option(Action<CreateIndexOptions<T>> option)
    {
        option(_options);
        return this;
    }

    /// <summary>
    /// Adds a key definition to the index
    /// <para>TIP: At least one key definition is required</para>
    /// </summary>
    /// <param name="propertyToIndex">x => x.PropertyName</param>
    /// <param name="type">The type of the key</param>
    public Index<T, TId> Key(Expression<Func<T, object>> propertyToIndex, KeyType type)
    {
        Keys.Add(new Key<T, TId>(propertyToIndex, type));
        return this;
    }

    /// <summary>
    /// Drops an index by name for this entity type
    /// </summary>
    /// <param name="name">The name of the index to drop</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task DropAsync(string name, CancellationToken cancellation = default)
    {
        await Collection.Indexes.DropOneAsync(name, cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops all indexes for this entity type
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task DropAllAsync(CancellationToken cancellation = default)
    {
        await Collection.Indexes.DropAllAsync(cancellation).ConfigureAwait(false);
    }

    private Task CreateAsync(CreateIndexModel<T> model, CancellationToken cancellation = default)
    {
        return Collection.Indexes.CreateOneAsync(model, cancellationToken: cancellation);
    }
}

internal class Key<T, TId>
    where TId : IComparable<TId>, IEquatable<TId>
    where T : IEntity<TId>
{
    internal string PropertyName { get; set; }
    internal KeyType Type { get; set; }

    internal Key(Expression<Func<T, object>> expression, KeyType type)
    {
        Type = type;

        if (expression.Body.NodeType == ExpressionType.Parameter && type == KeyType.Text)
        {
            PropertyName = "$**";
            return;
        }

        if (expression.Body.NodeType == ExpressionType.MemberAccess && type == KeyType.Text)
        {
            if (expression.PropertyInfo().PropertyType == typeof(FuzzyString))
                PropertyName = expression.FullPath() + ".Hash";
            else
                PropertyName = expression.FullPath();
            return;
        }

        PropertyName = expression.FullPath();
    }
}

public enum KeyType
{
    Ascending,
    Descending,
    Geo2D,
    Geo2DSphere,
    Hashed,
    Text,
    Wildcard
}
