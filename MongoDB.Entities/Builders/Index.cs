using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

/// <summary>
/// Represents an index creation command
/// <para>TIP: Define the keys first with .Key() method and finally call the .InitAsync() method.</para>
/// </summary>
/// <typeparam name="T">Any class that implements IEntity</typeparam>
public class Index<T>(DB db) where T : IEntity
{
    internal List<Key<T>> Keys { get; set; } = [];
    readonly CreateIndexOptions<T> _options = new() { Background = true };

    /// <summary>
    /// Call this method to finalize defining the index after setting the index keys and options.
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    /// <returns>The name of the created index</returns>
    public async Task<string> CreateAsync(CancellationToken cancellation = default)
    {
        if (Keys.Count == 0)
            throw new ArgumentException("Please define keys before calling this method.");

        var propNames = new List<string>();
        var keyDefs = new List<IndexKeysDefinition<T>>();
        var isTextIndex = false;

        foreach (var key in Keys)
        {
            var keyType = string.Empty;

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
            _options.Name = isTextIndex ? "[TEXT]" : string.Join(" | ", propNames);

        var model = new CreateIndexModel<T>(Builders<T>.IndexKeys.Combine(keyDefs), _options);

        try
        {
            await CreateAsync(model, cancellation).ConfigureAwait(false);
        }
        catch (MongoCommandException x) when (x.Code is 85 or 86)
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
    public Index<T> Option(Action<CreateIndexOptions<T>> option)
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
    public Index<T> Key(Expression<Func<T, object?>> propertyToIndex, KeyType type)
    {
        Keys.Add(new(propertyToIndex, type));

        return this;
    }

    /// <summary>
    /// Drops an index by name for this entity type
    /// </summary>
    /// <param name="name">The name of the index to drop</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task DropAsync(string name, CancellationToken cancellation = default)
    {
        await db.Collection<T>().Indexes.DropOneAsync(name, cancellation).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops all indexes for this entity type
    /// </summary>
    /// <param name="cancellation">An optional cancellation token</param>
    public async Task DropAllAsync(CancellationToken cancellation = default)
    {
        await db.Collection<T>().Indexes.DropAllAsync(cancellation).ConfigureAwait(false);
    }

    Task CreateAsync(CreateIndexModel<T> model, CancellationToken cancellation = default)
        => db.Collection<T>().Indexes.CreateOneAsync(model, cancellationToken: cancellation);
}

class Key<T> where T : IEntity
{
    internal string PropertyName { get; set; }
    internal KeyType Type { get; set; }

    internal Key(Expression<Func<T, object?>> expression, KeyType type)
    {
        Type = type;

        switch (expression.Body.NodeType)
        {
            case ExpressionType.Parameter when type == KeyType.Text:
                PropertyName = "$**";

                return;
            case ExpressionType.MemberAccess when type == KeyType.Text:
                PropertyName = expression.PropertyInfo().PropertyType == typeof(FuzzyString) ? expression.FullPath() + ".Hash" : expression.FullPath();

                return;
        }

        if (expression.Body is MethodCallExpression methodCallExpression &&
            methodCallExpression.Method.DeclaringType?.GetGenericTypeDefinition() == typeof(Dictionary<,>) &&
            methodCallExpression.Arguments.Count == 1 &&
            methodCallExpression.Arguments[0].Type == typeof(string) &&
            methodCallExpression.Arguments[0] is ConstantExpression constantExpression &&
            methodCallExpression.Object is MemberExpression memberExpression)
        {
            PropertyName = $"{memberExpression.Member.Name}.{constantExpression.Value}";

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