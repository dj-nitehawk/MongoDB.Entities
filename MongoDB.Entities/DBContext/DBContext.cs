using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace MongoDB.Entities
{
    /// <summary>
    /// This db context class can be used as an alternative entry point instead of the DB static class. 
    /// All methods on this class can be overriden if needed.
    /// </summary>
    public partial class DBContext
    {
        protected internal IClientSessionHandle session; //this will be set by Transaction class when inherited. otherwise null.

        private readonly ConcurrentDictionary<Type, (object filterDef, bool prepend)> globalFilters
            = new ConcurrentDictionary<Type, (object filterDef, bool prepend)>();

        /// <summary>
        /// The value of this property will be automatically set on entities when saving/updating if the entity has a ModifiedBy property
        /// </summary>
        public ModifiedBy ModifiedBy;

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
        public DBContext(string database, string host = "127.0.0.1", int port = 27017, ModifiedBy modifiedBy = null)
        {
            DB.Initialize(
                new MongoClientSettings { Server = new MongoServerAddress(host, port) },
                database,
                true)
              .GetAwaiter()
              .GetResult();

            ModifiedBy = modifiedBy;
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
        public DBContext(string database, MongoClientSettings settings, ModifiedBy modifiedBy = null)
        {
            DB.Initialize(settings, database, true)
              .GetAwaiter()
              .GetResult();

            ModifiedBy = modifiedBy;
        }

        /// <summary>
        /// Instantiates a DBContext instance
        /// <para>TIP: will throw an error if no connections have been initialized</para>
        /// </summary>
        /// <param name="modifiedBy">An optional ModifiedBy instance. 
        /// When supplied, all save/update operations performed via this DBContext instance will set the value on entities that has a property of type ModifiedBy. 
        /// You can even inherit from the ModifiedBy class and add your own properties to it. 
        /// Only one ModifiedBy property is allowed on a single entity type.</param>
        public DBContext(ModifiedBy modifiedBy = null)
            => ModifiedBy = modifiedBy;

        /// <summary>
        /// This event hook will be trigged right before an entity is persisted
        /// </summary>
        /// <typeparam name="T">Any entity that implements IEntity</typeparam>
        protected virtual Action<T> OnBeforeSave<T>() where T : IEntity
        {
            return null;
        }

        /// <summary>
        /// This event hook will be triggered right before an update/replace command is executed
        /// </summary>
        /// <typeparam name="T">Any entity that implements IEntity</typeparam>
        protected virtual Action<UpdateBase<T>> OnBeforeUpdate<T>() where T : IEntity
        {
            return null;
        }

        /// <summary>
        /// Specify a global filter to be applied to all operations performed with this DBContext
        /// </summary>
        /// <typeparam name="T">The type of Entity this global filter should be applied to</typeparam>
        /// <param name="filter">x => x.Prop1 == "some value"</param>
        /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param> 
        public void SetGlobalFilter<T>(Expression<Func<T, bool>> filter, bool prepend = false) where T : IEntity
        {
            SetGlobalFilter(Builders<T>.Filter.Where(filter), prepend);
        }

        /// <summary>
        /// Specify a global filter to be applied to all operations performed with this DBContext
        /// </summary>
        /// <typeparam name="T">The type of Entity this global filter should be applied to</typeparam>
        /// <param name="filter">b => b.Eq(x => x.Prop1, "some value")</param>
        /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
        public void SetGlobalFilter<T>(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter, bool prepend = false) where T : IEntity
        {
            SetGlobalFilter(filter(Builders<T>.Filter), prepend);
        }

        /// <summary>
        /// Specify a global filter to be applied to all operations performed with this DBContext
        /// </summary>
        /// <typeparam name="T">The type of Entity this global filter should be applied to</typeparam>
        /// <param name="filter">A filter definition to be applied</param>
        /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
        public void SetGlobalFilter<T>(FilterDefinition<T> filter, bool prepend = false) where T : IEntity
        {
            globalFilters[typeof(T)] = (filter, prepend);
        }

        /// <summary>
        /// Specify a global filter to be applied to all operations performed with this DBContext
        /// </summary>
        /// <param name="type">The type of Entity this global filter should be applied to</param>
        /// <param name="filter">A filter definition to be applied</param>
        /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
        public void SetGlobalFilter(Type type, object filter, bool prepend = false)
        {
            globalFilters[type] = (filter, prepend);
        }

        private void ThrowIfModifiedByIsEmpty<T>() where T : IEntity
        {
            if (Cache<T>.ModifiedByProp != null && ModifiedBy is null)
            {
                throw new InvalidOperationException(
                    $"A value for [{Cache<T>.ModifiedByProp.Name}] must be specified when saving/updating entities of type [{Cache<T>.CollectionName}]");
            }
        }
    }
}
