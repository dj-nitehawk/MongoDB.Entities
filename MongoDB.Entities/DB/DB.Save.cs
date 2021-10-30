using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        private static readonly BulkWriteOptions unOrdBlkOpts = new() { IsOrdered = false };
        private static readonly UpdateOptions updateOptions = new() { IsUpsert = true };

        /// <summary>
        /// Saves a complete entity replacing an existing entity or creating a new one if it does not exist. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">And optional cancellation token</param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static Task SaveAsync<T>(T entity, IClientSessionHandle session = null, CancellationToken cancellation = default, string tenantPrefix = null) where T : IEntity
        {
            if (PrepAndCheckIfInsert(entity))
            {
                return session == null
                       ? Collection<T>(tenantPrefix).InsertOneAsync(entity, null, cancellation)
                       : Collection<T>(tenantPrefix).InsertOneAsync(session, entity, null, cancellation);
            }

            return session == null
                   ? Collection<T>(tenantPrefix).ReplaceOneAsync(x => x.ID == entity.ID, entity, new ReplaceOptions { IsUpsert = true }, cancellation)
                   : Collection<T>(tenantPrefix).ReplaceOneAsync(session, x => x.ID == entity.ID, entity, new ReplaceOptions { IsUpsert = true }, cancellation);
        }

        /// <summary>
        /// Saves a batch of complete entities replacing existing ones or creating new ones if they do not exist. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">And optional cancellation token</param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static Task<BulkWriteResult<T>> SaveAsync<T>(IEnumerable<T> entities, IClientSessionHandle session = null, CancellationToken cancellation = default, string tenantPrefix = null) where T : IEntity
        {
            var models = new List<WriteModel<T>>(entities.Count());

            foreach (var ent in entities)
            {
                if (PrepAndCheckIfInsert(ent))
                {
                    models.Add(new InsertOneModel<T>(ent));
                }
                else
                {
                    models.Add(new ReplaceOneModel<T>(
                        filter: Builders<T>.Filter.Eq(e => e.ID, ent.ID),
                        replacement: ent)
                    { IsUpsert = true });
                }
            }
            return session == null
                   ? Collection<T>(tenantPrefix).BulkWriteAsync(models, unOrdBlkOpts, cancellation)
                   : Collection<T>(tenantPrefix).BulkWriteAsync(session, models, unOrdBlkOpts, cancellation);
        }

        /// <summary>
        /// Saves an entity partially with only the specified subset of properties. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>TIP: The properties to be saved can be specified with a 'New' expression. 
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static Task<UpdateResult> SaveOnlyAsync<T>(T entity, Expression<Func<T, object>> members, IClientSessionHandle session = null, CancellationToken cancellation = default, string tenantPrefix = null) where T : IEntity
        {
            return SavePartial(entity, members, tenantPrefix, session, cancellation);
        }

        /// <summary>
        /// Saves a batch of entities partially with only the specified subset of properties. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>TIP: The properties to be saved can be specified with a 'New' expression. 
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The batch of entities to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static Task<BulkWriteResult<T>> SaveOnlyAsync<T>(IEnumerable<T> entities, Expression<Func<T, object>> members, IClientSessionHandle session = null, CancellationToken cancellation = default, string tenantPrefix = null) where T : IEntity
        {
            return SavePartial(entities, members, tenantPrefix, session, cancellation);
        }

        /// <summary>
        /// Saves an entity partially excluding the specified subset of properties. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>TIP: The properties to be excluded can be specified with a 'New' expression. 
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static Task<UpdateResult> SaveExceptAsync<T>(T entity, Expression<Func<T, object>> members, IClientSessionHandle session = null, CancellationToken cancellation = default, string tenantPrefix = null) where T : IEntity
        {
            return SavePartial(entity, members, tenantPrefix, session, cancellation, true);
        }

        /// <summary>
        /// Saves a batch of entities partially excluding the specified subset of properties. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>TIP: The properties to be excluded can be specified with a 'New' expression. 
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The batch of entities to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static Task<BulkWriteResult<T>> SaveExceptAsync<T>(IEnumerable<T> entities, Expression<Func<T, object>> members, IClientSessionHandle session = null, CancellationToken cancellation = default, string tenantPrefix = null) where T : IEntity
        {
            return SavePartial(entities, members, tenantPrefix, session, cancellation, true);
        }

        /// <summary>
        /// Saves an entity partially while excluding some properties. 
        /// The properties to be excluded can be specified using the [Preserve] or [DontPreserve] attributes.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static Task<UpdateResult> SavePreservingAsync<T>(T entity, IClientSessionHandle session = null, CancellationToken cancellation = default, string tenantPrefix = null) where T : IEntity
        {
            entity.ThrowIfUnsaved();

            var propsToUpdate = Cache<T>.UpdatableProps(entity);

            IEnumerable<string> propsToPreserve = new string[0];

            var dontProps = propsToUpdate.Where(p => p.IsDefined(typeof(DontPreserveAttribute), false)).Select(p => p.Name);
            var presProps = propsToUpdate.Where(p => p.IsDefined(typeof(PreserveAttribute), false)).Select(p => p.Name);

            if (dontProps.Any() && presProps.Any())
                throw new NotSupportedException("[Preseve] and [DontPreserve] attributes cannot be used together on the same entity!");

            if (dontProps.Any())
                propsToPreserve = propsToUpdate.Where(p => !dontProps.Contains(p.Name)).Select(p => p.Name);

            if (presProps.Any())
                propsToPreserve = propsToUpdate.Where(p => presProps.Contains(p.Name)).Select(p => p.Name);

            if (!propsToPreserve.Any())
                throw new ArgumentException("No properties are being preserved. Please use .SaveAsync() method instead!");

            propsToUpdate = propsToUpdate.Where(p => !propsToPreserve.Contains(p.Name));

            var propsToUpdateCount = propsToUpdate.Count();

            if (propsToUpdateCount == 0)
                throw new ArgumentException("At least one property must be not preserved!");

            var defs = new List<UpdateDefinition<T>>(propsToUpdateCount);

            foreach (var p in propsToUpdate)
            {
                if (p.Name == Cache<T>.ModifiedOnPropName)
                    defs.Add(Builders<T>.Update.CurrentDate(Cache<T>.ModifiedOnPropName));
                else
                    defs.Add(Builders<T>.Update.Set(p.Name, p.GetValue(entity)));
            }

            return
                session == null
                ? Collection<T>(tenantPrefix).UpdateOneAsync(e => e.ID == entity.ID, Builders<T>.Update.Combine(defs), updateOptions, cancellation)
                : Collection<T>(tenantPrefix).UpdateOneAsync(session, e => e.ID == entity.ID, Builders<T>.Update.Combine(defs), updateOptions, cancellation);
        }

        private static Task<UpdateResult> SavePartial<T>(T entity, Expression<Func<T, object>> members, string tenantPrefix, IClientSessionHandle session, CancellationToken cancellation, bool excludeMode = false) where T : IEntity
        {
            PrepAndCheckIfInsert(entity); //just prep. we don't care about inserts here
            return
                session == null
                ? Collection<T>(tenantPrefix).UpdateOneAsync(e => e.ID == entity.ID, Builders<T>.Update.Combine(Logic.BuildUpdateDefs(entity, members, excludeMode)), updateOptions, cancellation)
                : Collection<T>(tenantPrefix).UpdateOneAsync(session, e => e.ID == entity.ID, Builders<T>.Update.Combine(Logic.BuildUpdateDefs(entity, members, excludeMode)), updateOptions, cancellation);
        }

        private static Task<BulkWriteResult<T>> SavePartial<T>(IEnumerable<T> entities, Expression<Func<T, object>> members, string tenantPrefix, IClientSessionHandle session, CancellationToken cancellation, bool excludeMode = false) where T : IEntity
        {
            var models = new List<WriteModel<T>>(entities.Count());

            foreach (var ent in entities)
            {
                PrepAndCheckIfInsert(ent); //just prep. we don't care about inserts here
                models.Add(
                    new UpdateOneModel<T>(
                            filter: Builders<T>.Filter.Eq(e => e.ID, ent.ID),
                            update: Builders<T>.Update.Combine(Logic.BuildUpdateDefs(ent, members, excludeMode)))
                    { IsUpsert = true });
            }

            return session == null
                ? Collection<T>(tenantPrefix).BulkWriteAsync(models, unOrdBlkOpts, cancellation)
                : Collection<T>(tenantPrefix).BulkWriteAsync(session, models, unOrdBlkOpts, cancellation);
        }

        private static bool PrepAndCheckIfInsert<T>(T entity) where T : IEntity
        {
            if (string.IsNullOrEmpty(entity.ID))
            {
                entity.ID = entity.GenerateNewID();
                if (Cache<T>.HasCreatedOn) ((ICreatedOn)entity).CreatedOn = DateTime.UtcNow;
                if (Cache<T>.HasModifiedOn) ((IModifiedOn)entity).ModifiedOn = DateTime.UtcNow;
                return true;
            }

            if (Cache<T>.HasModifiedOn) ((IModifiedOn)entity).ModifiedOn = DateTime.UtcNow;
            return false;
        }
    }
}
