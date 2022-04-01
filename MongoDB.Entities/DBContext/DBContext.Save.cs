using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DBContext
    {

        /// <summary>
        /// Saves a complete entity replacing an existing entity or creating a new one if it does not exist. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TId">ID type</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="cancellation">And optional cancellation token</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public Task SaveAsync<T, TId>(T entity, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
            where TId : IComparable<TId>, IEquatable<TId>
            where T : IEntity<TId>
        {
            SetModifiedBySingle(entity);
            OnBeforeSave(entity);
            collection = Collection(collectionName, collection);
            if (PrepAndCheckIfInsert<T, TId>(entity))
            {
                return Session is null
                       ? collection.InsertOneAsync(entity, null, cancellation)
                       : collection.InsertOneAsync(Session, entity, null, cancellation);
            }
            var filter = Builders<T>.Filter.Eq(x => x.ID, entity.ID);
            return Session == null
                   ? collection.ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = true }, cancellation)
                   : collection.ReplaceOneAsync(Session, filter, entity, new ReplaceOptions { IsUpsert = true }, cancellation);
        }

        /// <summary>
        /// Saves a complete entity replacing an existing entity or creating a new one if it does not exist. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="cancellation">And optional cancellation token</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public Task SaveAsync<T>(T entity, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
            where T : IEntity => SaveAsync<T, string>(entity, cancellation, collectionName, collection);


        /// <summary>
        /// Saves a batch of complete entities replacing an existing entities or creating a new ones if they do not exist. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TId">ID type</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="cancellation">And optional cancellation token</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public Task<BulkWriteResult<T>> SaveAsync<T, TId>(IEnumerable<T> entities, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
            where TId : IComparable<TId>, IEquatable<TId>
            where T : IEntity<TId>
        {
            SetModifiedByMultiple(entities);
            foreach (var ent in entities) OnBeforeSave(ent);


            var models = entities.Select<T, WriteModel<T>>(ent =>
            {
                if (PrepAndCheckIfInsert<T, TId>(ent))
                {
                    return new InsertOneModel<T>(ent);
                }
                else
                {
                    return new ReplaceOneModel<T>(
                        filter: Builders<T>.Filter.Eq(e => e.ID, ent.ID),
                        replacement: ent)
                    { IsUpsert = true };
                }
            });

            return Session == null
                   ? Collection(collectionName, collection).BulkWriteAsync(models, _unOrdBlkOpts, cancellation)
                   : Collection(collectionName, collection).BulkWriteAsync(Session, models, _unOrdBlkOpts, cancellation);
        }


        /// <summary>
        /// Saves a batch of complete entities replacing an existing entities or creating a new ones if they do not exist. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is replaced.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="cancellation">And optional cancellation token</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public Task<BulkWriteResult<T>> SaveAsync<T>(IEnumerable<T> entities, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
            where T : IEntity
        {
            return SaveAsync<T, string>(entities: entities, cancellation: cancellation, collectionName: collectionName, collection: collection);
        }

        /// <summary>
        /// Saves an entity partially with only the specified subset of properties. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>TIP: The properties to be saved can be specified with a 'New' expression. 
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TId">ID type</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public Task<UpdateResult> SaveOnlyAsync<T, TId>(T entity, Expression<Func<T, object>> members, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
            where TId : IComparable<TId>, IEquatable<TId>
            where T : IEntity<TId>
        {
            SetModifiedBySingle(entity);
            OnBeforeSave(entity);
            return SavePartial<T, TId>(entity, members, cancellation, collectionName: collectionName, collection: collection);

        }

        /// <summary>
        /// Saves a batch of entities partially with only the specified subset of properties. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>TIP: The properties to be saved can be specified with a 'New' expression. 
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TId">ID type</typeparam>
        /// <param name="entities">The batch of entities to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public Task<BulkWriteResult<T>> SaveOnlyAsync<T, TId>(IEnumerable<T> entities, Expression<Func<T, object>> members, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
            where TId : IComparable<TId>, IEquatable<TId>
            where T : IEntity<TId>
        {
            SetModifiedByMultiple(entities);
            foreach (var ent in entities) OnBeforeSave(ent);
            return SavePartial<T, TId>(entities, members, cancellation, collectionName: collectionName, collection: collection);
        }

        /// <summary>
        /// Saves an entity partially excluding the specified subset of properties. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>TIP: The properties to be excluded can be specified with a 'New' expression. 
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TId">ID type</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public Task<UpdateResult> SaveExceptAsync<T, TId>(T entity, Expression<Func<T, object>> members, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
            where TId : IComparable<TId>, IEquatable<TId>
            where T : IEntity<TId>
        {
            SetModifiedBySingle(entity);
            OnBeforeSave(entity);
            return SavePartial<T, TId>(entity, members, cancellation, true, collectionName: collectionName, collection: collection);
        }

        /// <summary>
        /// Saves a batch of entities partially excluding the specified subset of properties. 
        /// If ID value is null, a new entity is created. If ID has a value, then existing entity is updated.
        /// <para>TIP: The properties to be excluded can be specified with a 'New' expression. 
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TId">ID type</typeparam>
        /// <param name="entities">The batch of entities to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public Task<BulkWriteResult<T>> SaveExceptAsync<T, TId>(IEnumerable<T> entities, Expression<Func<T, object>> members, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
            where TId : IComparable<TId>, IEquatable<TId>
            where T : IEntity<TId>
        {
            SetModifiedByMultiple(entities);
            foreach (var ent in entities) OnBeforeSave(ent);
            return SavePartial<T, TId>(entities, members, cancellation, true, collectionName: collectionName, collection: collection);
        }

        /// <summary>
        /// Saves an entity partially while excluding some properties
        /// The properties to be excluded can be specified using the [Preserve] or [DontPreserve] attributes.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TId">ID type</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="collectionName"></param>
        /// <param name="collection"></param>
        public Task<UpdateResult> SavePreservingAsync<T, TId>(T entity, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null)
            where TId : IComparable<TId>, IEquatable<TId>
            where T : IEntity<TId>
        {
            SetModifiedBySingle(entity);
            OnBeforeSave(entity);
            entity.ThrowIfUnsaved();
            var cache = Cache<T>();
            var propsToUpdate = cache.UpdatableProps(entity);

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
                if (p.Name == cache.ModifiedOnPropName)
                    defs.Add(Builders<T>.Update.CurrentDate(cache.ModifiedOnPropName));
                else
                    defs.Add(Builders<T>.Update.Set(p.Name, p.GetValue(entity)));
            }

            var filter = Builders<T>.Filter.Eq(e => e.ID, entity.ID);
            var update = Builders<T>.Update.Combine(defs);
            return
                Session == null
                ? Collection(collectionName, collection).UpdateOneAsync(filter, update, _updateOptions, cancellation)
                : Collection(collectionName, collection).UpdateOneAsync(Session, filter, update, _updateOptions, cancellation);
        }

        private void SetModifiedBySingle<T>(T entity)
        {
            var cache = Cache<T>();
            if (cache.ModifiedByProp is null)
                return;
            ThrowIfModifiedByIsEmpty<T>();

            cache.ModifiedByProp.SetValue(
                entity,
                BsonSerializer.Deserialize(ModifiedBy.ToBson(), cache.ModifiedByProp.PropertyType));
            //note: we can't use an IModifiedBy interface because the above line needs a concrete type
            //      to be able to correctly deserialize a user supplied derived/sub class of ModifiedOn.
        }

        private void SetModifiedByMultiple<T>(IEnumerable<T> entities)
        {
            var cache = Cache<T>();
            if (cache.ModifiedByProp is null)
                return;

            ThrowIfModifiedByIsEmpty<T>();

            var val = BsonSerializer.Deserialize(ModifiedBy.ToBson(), cache.ModifiedByProp.PropertyType);

            foreach (var e in entities)
                cache.ModifiedByProp.SetValue(e, val);
        }
    }
}
