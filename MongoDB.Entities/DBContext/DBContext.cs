using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
#nullable enable
namespace MongoDB.Entities
{
    /// <summary>
    /// This db context class can be used as an alternative entry point instead of the DB static class. 
    /// </summary>
    public partial class DBContext : IMongoDatabase
    {
        /// <summary>
        /// Returns the session object used for transactions
        /// </summary>
        public IClientSessionHandle? Session { get; protected set; }


        public MongoServerContext MongoServerContext { get; set; }
        public IMongoDatabase Database { get; set; }
        public DBContextOptions Options { get; set; }

        /// <summary>
        /// wrapper around <see cref="MongoServerContext.ModifiedBy"/> so that we don't break the public api
        /// </summary>
        public ModifiedBy? ModifiedBy
        {
            get
            {
                return MongoServerContext.ModifiedBy;
            }
            [Obsolete("Use MongoContext.Options.ModifiedBy = value instead")]
            set
            {
                MongoServerContext.Options.ModifiedBy = value;
            }
        }

        public IMongoClient Client => MongoServerContext;

        public DatabaseNamespace DatabaseNamespace => Database.DatabaseNamespace;

        public MongoDatabaseSettings Settings => Database.Settings;

        private Dictionary<Type, (object filterDef, bool prepend)>? _globalFilters;
        private Dictionary<Type, (object filterDef, bool prepend)> GlobalFilters => _globalFilters ??= new();

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="other"></param>
        public DBContext(DBContext other)
        {
            MongoServerContext = other.MongoServerContext;
            Database = other.Database;
            Options = other.Options;
        }
        public DBContext(MongoServerContext mongoContext, IMongoDatabase database, DBContextOptions? options = null)
        {
            MongoServerContext = mongoContext;
            Database = database;
            Options = options ?? new();
        }
        public DBContext(MongoServerContext mongoContext, string database, MongoDatabaseSettings? settings = null, DBContextOptions? options = null)
        {
            MongoServerContext = mongoContext;
            Options = options ?? new();
            Database = mongoContext.GetDatabase(database, settings);
        }

        /// <summary>
        /// Initializes a DBContext instance with the given connection parameters.
        /// <para>TIP: network connection is deferred until the first actual operation.</para>
        /// </summary>
        /// <param name="database">Name of the database</param>
        /// <param name="host">Address of the MongoDB server</param>
        /// <param name="port">Port number of the server</param>
        /// <param name="modifiedBy">An optional ModifiedBy instance. 
        /// When supplied, all save/update operations performed via this DBContext instance will set the value on entities that has a property of type ModifiedBy. 
        /// You can even inherit from the ModifiedBy class and add your own properties to it. 
        /// Only one ModifiedBy property is allowed on a single entity type.</param>
        public DBContext(string database, string host = "127.0.0.1", int port = 27017, ModifiedBy? modifiedBy = null)
        {
            MongoServerContext = new MongoServerContext(
                client: new MongoClient(
                    new MongoClientSettings
                    {
                        Server = new MongoServerAddress(host, port)
                    }),
                options: new()
                {
                    ModifiedBy = modifiedBy
                });
            Options = new();
            Database = MongoServerContext.GetDatabase(database);
        }

        /// <summary>
        /// Initializes a DBContext instance with the given connection parameters.
        /// <para>TIP: network connection is deferred until the first actual operation.</para>
        /// </summary>
        /// <param name="database">Name of the database</param>
        /// <param name="settings">A MongoClientSettings object</param>
        /// <param name="modifiedBy">An optional ModifiedBy instance. 
        /// When supplied, all save/update operations performed via this DBContext instance will set the value on entities that has a property of type ModifiedBy. 
        /// You can even inherit from the ModifiedBy class and add your own properties to it. 
        /// Only one ModifiedBy property is allowed on a single entity type.</param>
        public DBContext(string database, MongoClientSettings settings, ModifiedBy? modifiedBy = null)
        {
            MongoServerContext = new MongoServerContext(
               client: new MongoClient(settings),
               options: new()
               {
                   ModifiedBy = modifiedBy
               });
            Options = new();
            Database = MongoServerContext.GetDatabase(database);
        }


        /// <summary>
        /// Instantiates a DBContext instance
        /// <para>TIP: will throw an error if no connections have been initialized</para>
        /// </summary>
        /// <param name="modifiedBy">An optional ModifiedBy instance. 
        /// When supplied, all save/update operations performed via this DBContext instance will set the value on entities that has a property of type ModifiedBy. 
        /// You can even inherit from the ModifiedBy class and add your own properties to it. 
        /// Only one ModifiedBy property is allowed on a single entity type.</param>
        [Obsolete("This constructor is obsolete, you can only create a DBContext after knowing the database name")]
        public DBContext(ModifiedBy? modifiedBy = null) : this("default", modifiedBy: modifiedBy)
        {
        }


        /// <summary>
        /// This event hook will be trigged right before an entity is persisted
        /// </summary>
        /// <typeparam name="T">Any entity that implements IEntity</typeparam>
        protected virtual Action<T>? OnBeforeSave<T>() where T : IEntity
        {
            return null;
        }

        /// <summary>
        /// This event hook will be triggered right before an update/replace command is executed
        /// </summary>
        /// <typeparam name="T">Any entity that implements IEntity</typeparam>
        /// <typeparam name="TSelf">Any entity that implements IEntity</typeparam>
        protected virtual Action<UpdateBase<T, TSelf>>? OnBeforeUpdate<T, TSelf>() where T : IEntity where TSelf : UpdateBase<T, TSelf>
        {
            return null;
        }



        /// <summary>
        /// Specify a global filter to be applied to all operations performed with this DBContext
        /// </summary>
        /// <typeparam name="T">The type of Entity this global filter should be applied to</typeparam>
        /// <param name="filter">x => x.Prop1 == "some value"</param>
        /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param> 
        protected void SetGlobalFilter<T>(Expression<Func<T, bool>> filter, bool prepend = false) where T : IEntity
        {
            SetGlobalFilter(Builders<T>.Filter.Where(filter), prepend);
        }

        /// <summary>
        /// Specify a global filter to be applied to all operations performed with this DBContext
        /// </summary>
        /// <typeparam name="T">The type of Entity this global filter should be applied to</typeparam>
        /// <param name="filter">b => b.Eq(x => x.Prop1, "some value")</param>
        /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
        protected void SetGlobalFilter<T>(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter, bool prepend = false) where T : IEntity
        {
            SetGlobalFilter(filter(Builders<T>.Filter), prepend);
        }

        /// <summary>
        /// Specify a global filter to be applied to all operations performed with this DBContext
        /// </summary>
        /// <typeparam name="T">The type of Entity this global filter should be applied to</typeparam>
        /// <param name="filter">A filter definition to be applied</param>
        /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
        protected void SetGlobalFilter<T>(FilterDefinition<T> filter, bool prepend = false) where T : IEntity
        {
            AddFilter(typeof(T), (filter, prepend));
        }

        /// <summary>
        /// Specify a global filter to be applied to all operations performed with this DBContext
        /// </summary>
        /// <param name="type">The type of Entity this global filter should be applied to</param>
        /// <param name="jsonString">A JSON string filter definition to be applied</param>
        /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
        protected void SetGlobalFilter(Type type, string jsonString, bool prepend = false)
        {
            AddFilter(type, (jsonString, prepend));
        }



        /// <summary>
        /// Specify a global filter to be applied to all operations performed with this DBContext
        /// </summary>
        /// <typeparam name="TBase">The type of the base class</typeparam>
        /// <param name="filter">b => b.Eq(x => x.Prop1, "some value")</param>
        /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
        protected void SetGlobalFilterForBaseClass<TBase>(Expression<Func<TBase, bool>> filter, bool prepend = false) where TBase : IEntity
        {
            SetGlobalFilterForBaseClass(Builders<TBase>.Filter.Where(filter), prepend);
        }

        /// <summary>
        /// Specify a global filter to be applied to all operations performed with this DBContext
        /// </summary>
        /// <typeparam name="TBase">The type of the base class</typeparam>
        /// <param name="filter">b => b.Eq(x => x.Prop1, "some value")</param>
        /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
        protected void SetGlobalFilterForBaseClass<TBase>(Func<FilterDefinitionBuilder<TBase>, FilterDefinition<TBase>> filter, bool prepend = false) where TBase : IEntity
        {
            SetGlobalFilterForBaseClass(filter(Builders<TBase>.Filter), prepend);
        }

        /// <summary>
        /// Specify a global filter to be applied to all operations performed with this DBContext
        /// </summary>
        /// <typeparam name="TBase">The type of the base class</typeparam>
        /// <param name="filter">A filter definition to be applied</param>
        /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
        protected void SetGlobalFilterForBaseClass<TBase>(FilterDefinition<TBase> filter, bool prepend = false) where TBase : IEntity
        {
            foreach (var entType in MongoServerContext.AllEntitiyTypes.Where(t => t.IsSubclassOf(typeof(TBase))))
            {
                var bsonDoc = filter.Render(
                    BsonSerializer.SerializerRegistry.GetSerializer<TBase>(),
                    BsonSerializer.SerializerRegistry);

                AddFilter(entType, (bsonDoc, prepend));
            }
        }

        /// <summary>
        /// Specify a global filter for all entity types that implements a given interface
        /// </summary>
        /// <typeparam name="TInterface">The interface type to target. Will throw if supplied argument is not an interface type</typeparam>
        /// <param name="jsonString">A JSON string filter definition to be applied</param>
        /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
        protected void SetGlobalFilterForInterface<TInterface>(string jsonString, bool prepend = false)
        {
            var targetType = typeof(TInterface);

            if (!targetType.IsInterface) throw new ArgumentException("Only interfaces are allowed!", "TInterface");


            foreach (var entType in MongoServerContext.AllEntitiyTypes.Where(t => targetType.IsAssignableFrom(t)))
            {
                AddFilter(entType, (jsonString, prepend));
            }
        }




        private void ThrowIfModifiedByIsEmpty<T>() where T : IEntity
        {
            if (Cache<T>().ModifiedByProp != null && ModifiedBy is null)
            {
                throw new InvalidOperationException(
                    $"A value for [{Cache<T>().ModifiedByProp.Name}] must be specified when saving/updating entities of type [{Cache<T>().CollectionName}]");
            }
        }

        private void AddFilter(Type type, (object filterDef, bool prepend) filter)
        {
            GlobalFilters[type] = filter;
        }


        private readonly ConcurrentDictionary<Type, Cache> _cache = new();
        internal Cache<T> Cache<T>() where T : IEntity
        {
            if (!_cache.TryGetValue(typeof(T), out var c))
            {
                c = new Cache<T>();
            }
            return (Cache<T>)c;
        }

        public IMongoCollection<T> CollectionFor<T>() where T : IEntity
        {
            return Database.GetCollection<T>(Cache<T>().CollectionName);
        }
    }
}
