using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents an index for a given Entity
    /// <para>TIP: Define the keys first with .Key() method and finally call the .Create() method.</para>
    /// </summary>
    /// <typeparam name="T">Any class that inherits from Entity</typeparam>
    public class Index<T> where T : Entity
    {
        internal HashSet<Key<T>> Keys { get; set; } = new HashSet<Key<T>>();

        private Options _options = new Options { Background = true };

        /// <summary>
        /// Call this method to finalize defining the index after setting the index keys and options.
        /// </summary>
        public void Create()
        {
            CreateAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Call this method to finalize defining the index after setting the index keys and options.
        /// </summary>
        async public Task CreateAsync()
        {
            if (Keys.Count == 0) throw new ArgumentException("Please define keys before calling this method.");

            var propNames = new HashSet<string>();
            var keyDefs = new HashSet<IndexKeysDefinition<T>>();
            var isTextIndex = false;

            foreach (var key in Keys)
            {
                switch (key.Type)
                {
                    case Type.Ascending:
                        keyDefs.Add(Builders<T>.IndexKeys.Ascending(key.Property));
                        break;
                    case Type.Descending:
                        keyDefs.Add(Builders<T>.IndexKeys.Descending(key.Property));
                        break;
                    case Type.Geo2D:
                        keyDefs.Add(Builders<T>.IndexKeys.Geo2D(key.Property));
                        break;
                    case Type.Geo2DSphere:
                        keyDefs.Add(Builders<T>.IndexKeys.Geo2DSphere(key.Property));
                        break;
                    case Type.GeoHaystack:
                        keyDefs.Add(Builders<T>.IndexKeys.GeoHaystack(key.Property));
                        break;
                    case Type.Hashed:
                        keyDefs.Add(Builders<T>.IndexKeys.Hashed(key.Property));
                        break;
                    case Type.Text:
                        keyDefs.Add(Builders<T>.IndexKeys.Text(key.Property));
                        isTextIndex = true;
                        break;
                }

                var member = key.Property.Body as MemberExpression;
                if (member == null) member = (key.Property.Body as UnaryExpression)?.Operand as MemberExpression;
                if (member == null) throw new ArgumentException("Unable to get property name");
                propNames.Add(member.Member.Name);
            }

            if (string.IsNullOrEmpty(_options.Name))
            {
                _options.Name = DB.GetCollectionName<T>();
                if (isTextIndex)
                {
                    _options.Name = $"{_options.Name}[TEXT]";
                }
                else
                {
                    _options.Name = $"{_options.Name}[{string.Join("-", propNames)}]";
                }
            }

            var model = new CreateIndexModel<T>(
                                Builders<T>.IndexKeys.Combine(keyDefs),
                                _options.ToCreateIndexOptions());
            try
            {
                await DB.CreateIndexAsync<T>(model);
            }
            catch (MongoCommandException x)
            {
                if (x.Code == 85 || x.Code == 86)
                {
                    await DB.DropIndexAsync<T>(_options.Name);
                    await DB.CreateIndexAsync<T>(model);
                }
                else
                {
                    throw x;
                }
            }
        }

        /// <summary>
        /// Set the options for this index definition
        /// <para>TIP: Setting options is not required.</para>
        /// </summary>
        /// <param name="options">x => x.Option1 = Value1, x => x.Option2 = Value2</param>
        public Index<T> Options(params Action<Options>[] options)
        {
            foreach (var opt in options)
            {
                opt(_options);
            }

            return this;
        }

        /// <summary>
        /// Adds a key definition to the index
        /// <para>TIP: At least one key definition is required</para>
        /// </summary>
        /// <param name="propertyToIndex">x => x.PropertyName</param>
        /// <param name="type">The type of the key</param>
        public Index<T> Key(Expression<Func<T, object>> propertyToIndex, Type type)
        {
            Keys.Add(new Key<T>(propertyToIndex, type));
            return this;
        }
    }

    internal class Key<T> where T : Entity
    {
        internal Expression<Func<T, object>> Property { get; set; }
        internal Type Type { get; set; }

        internal Key(Expression<Func<T, object>> prop, Type type)
        {
            Property = prop;
            Type = type;
        }
    }

    public enum Type
    {
        Ascending,
        Descending,
        Geo2D,
        Geo2DSphere,
        GeoHaystack,
        Hashed,
        Text
    }

    public class Options : CreateIndexOptions
    {
        internal CreateIndexOptions ToCreateIndexOptions()
        {
            return BsonSerializer.Deserialize<CreateIndexOptions>(this.ToBson());
        }
    }
}
