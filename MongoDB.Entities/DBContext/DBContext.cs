using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        /// Instantiates a DBContext
        /// </summary>
        /// <param name="modifiedBy">An optional ModifiedBy instance. 
        /// When supplied, all save/update operations performed via this DBContext instance will set the value on entities that has a property of type ModifiedBy. 
        /// You can even inherit from the ModifiedBy class and add your own properties to it. 
        /// Only one ModifiedBy property is allowed on a single entity type.</param>
        public DBContext(ModifiedBy modifiedBy = null)
            => ModifiedBy = modifiedBy;
        //todo: write tests cases for the custom hooks
        /// <summary>
        /// A custom hook for modifying all replace commands before they are executed.
        /// <para>TIP: You can access the entity instance via <c>replace.Entity</c> property.</para>
        /// </summary>
        /// <typeparam name="T">Any entity type that implement IEntity</typeparam>
        /// <param name="replace">The replace command that is about to be executed</param>
        public virtual Replace<T> OnBeforeReplace<T>(Replace<T> replace) where T : IEntity
        {
            return replace;
        }

        /// <summary>
        /// A custom hook for modifying entities before they are saved.
        /// <para>TIP: check if the ID property is null to determine if the entity is being inserted</para>
        /// </summary>
        /// <typeparam name="T">Any entity type that implement IEntity</typeparam>
        /// <param name="entities">A collection of entities that are about to be saved</param>
        public virtual void OnBeforeSave<T>(IEnumerable<T> entities) where T : IEntity { }

        /// <summary>
        /// A custom hook for modifying all update commands before they are executed.
        /// <para>WARNING: Batch updates (AddToQueue) and pipeline updates (WithPipelineStage) won't trigger this hook</para>
        /// </summary>
        /// <typeparam name="T">Any entity type that implement IEntity</typeparam>
        /// <param name="update">Use <c>update.AddModification()</c> to add modifications to the update</param>
        public virtual UpdateBase<T> OnBeforeUpdate<T>(UpdateBase<T> update) where T : IEntity
        {
            return update;
        }

        /// <summary>
        /// Specify a global filter to be applied to all operations performed with this DBContext
        /// </summary>
        /// <typeparam name="T">The type of Entity this globa filter should be applied to</typeparam>
        /// <param name="filter">x => x.Prop1 == "some value"</param>
        /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param> 
        public void SetGlobalFilter<T>(Expression<Func<T, bool>> filter, bool prepend = false) where T : IEntity
        {
            SetGlobalFilter(Builders<T>.Filter.Where(filter), prepend);
        }

        /// <summary>
        /// Specify a global filter to be applied to all operations performed with this DBContext
        /// </summary>
        /// <typeparam name="T">The type of Entity this globa filter should be applied to</typeparam>
        /// <param name="filter">b => b.Eq(x => x.Prop1, "some value")</param>
        /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
        public void SetGlobalFilter<T>(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter, bool prepend = false) where T : IEntity
        {
            SetGlobalFilter(filter(Builders<T>.Filter), prepend);
        }

        /// <summary>
        /// Specify a global filter to be applied to all operations performed with this DBContext
        /// </summary>
        /// <typeparam name="T">The type of Entity this globa filter should be applied to</typeparam>
        /// <param name="filter">A filter definition to be applied</param>
        /// <param name="prepend">Set to true if you want to prepend this global filter to your operation filters instead of being appended</param>
        public void SetGlobalFilter<T>(FilterDefinition<T> filter, bool prepend = false) where T : IEntity
        {
            globalFilters[typeof(T)] = (filter, prepend);
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
