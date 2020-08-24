using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents an index creation command
    /// <para>TIP: Define the keys first with .Key() method and finally call the .Create() method.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public class Index<T> where T : IEntity
    {
        internal HashSet<Key<T>> Keys { get; set; } = new HashSet<Key<T>>();
        private readonly CreateIndexOptions<T> options = new CreateIndexOptions<T> { Background = true };

        /// <summary>
        /// Call this method to finalize defining the index after setting the index keys and options.
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public async Task CreateAsync(CancellationToken cancellation = default)
        {
            if (Keys.Count == 0) throw new ArgumentException("Please define keys before calling this method.");

            var propNames = new HashSet<string>();
            var keyDefs = new HashSet<IndexKeysDefinition<T>>();
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

            if (string.IsNullOrEmpty(options.Name))
            {
                if (isTextIndex)
                {
                    options.Name = "[TEXT]";
                }
                else
                {
                    options.Name = string.Join(" | ", propNames);
                }
            }

            var model = new CreateIndexModel<T>(
                Builders<T>.IndexKeys.Combine(keyDefs),
                options);

            try
            {
                await DB.CreateIndexAsync(model, cancellation).ConfigureAwait(false);
            }
            catch (MongoCommandException x) when (x.Code == 85 || x.Code == 86)
            {
                await DB.DropIndexAsync<T>(options.Name, cancellation).ConfigureAwait(false);
                await DB.CreateIndexAsync(model, cancellation).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Set the options for this index definition
        /// <para>TIP: Setting options is not required.</para>
        /// </summary>
        /// <param name="option">x => x.OptionName = OptionValue</param>
        public Index<T> Option(Action<CreateIndexOptions<T>> option)
        {
            option(options);
            return this;
        }

        /// <summary>
        /// Adds a key definition to the index
        /// <para>TIP: At least one key definition is required</para>
        /// </summary>
        /// <param name="propertyToIndex">x => x.PropertyName</param>
        /// <param name="type">The type of the key</param>
        public Index<T> Key(Expression<Func<T, object>> propertyToIndex, KeyType type)
        {
            Keys.Add(new Key<T>(propertyToIndex, type));
            return this;
        }
    }

    internal class Key<T> where T : IEntity
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
                var propType = ((expression.Body as MemberExpression)?.Member as PropertyInfo)?.PropertyType;

                if (propType == typeof(FuzzyString))
                {
                    PropertyName = expression.FullPath() + ".Hash";
                }
                else
                {
                    PropertyName = expression.FullPath();
                }
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
}
