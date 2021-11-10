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
        private static readonly BulkWriteOptions _unOrdBlkOpts = new() { IsOrdered = false };
        private static readonly UpdateOptions _updateOptions = new() { IsUpsert = true };
        private Task<UpdateResult> SavePartial<T>(T entity, Expression<Func<T, object>> members, CancellationToken cancellation, bool excludeMode = false, string? collectionName = null, IMongoCollection<T>? collection = null) where T : IEntity
        {
            PrepAndCheckIfInsert(entity); //just prep. we don't care about inserts here
            return
                Session == null
                ? Collection(collectionName, collection).UpdateOneAsync(e => e.ID == entity.ID, Builders<T>.Update.Combine(Logic.BuildUpdateDefs(entity, members, excludeMode)), _updateOptions, cancellation)
                : Collection(collectionName, collection).UpdateOneAsync(Session, e => e.ID == entity.ID, Builders<T>.Update.Combine(Logic.BuildUpdateDefs(entity, members, excludeMode)), _updateOptions, cancellation);
        }

        private Task<BulkWriteResult<T>> SavePartial<T>(IEnumerable<T> entities, Expression<Func<T, object>> members, CancellationToken cancellation, bool excludeMode = false, string? collectionName = null, IMongoCollection<T>? collection = null) where T : IEntity
        {
            var models = entities.Select(ent =>
            {
                PrepAndCheckIfInsert(ent); //just prep. we don't care about inserts here
                return new UpdateOneModel<T>(
                            filter: Builders<T>.Filter.Eq(e => e.ID, ent.ID),
                            update: Builders<T>.Update.Combine(Logic.BuildUpdateDefs(ent, members, excludeMode)))
                { IsUpsert = true };
            }).ToList();
            return Session == null
                ? Collection(collectionName, collection).BulkWriteAsync(models, _unOrdBlkOpts, cancellation)
                : Collection(collectionName, collection).BulkWriteAsync(Session, models, _unOrdBlkOpts, cancellation);
        }

        private bool PrepAndCheckIfInsert<T>(T entity) where T : IEntity
        {
            var cache = Cache<T>();
            if (string.IsNullOrEmpty(entity.ID))
            {
                entity.ID = entity.GenerateNewID();
                if (cache.HasCreatedOn) ((ICreatedOn)entity).CreatedOn = DateTime.UtcNow;
                if (cache.HasModifiedOn) ((IModifiedOn)entity).ModifiedOn = DateTime.UtcNow;
                return true;
            }

            if (cache.HasModifiedOn) ((IModifiedOn)entity).ModifiedOn = DateTime.UtcNow;
            return false;
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
        public Task InsertAsync<T>(T entity, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null) where T : IEntity
        {
            SetModifiedBySingle(entity);
            OnBeforeSave(entity);
            PrepAndCheckIfInsert(entity);
            return Session == null
                   ? Collection(collectionName, collection).InsertOneAsync(entity, null, cancellation)
                   : Collection(collectionName, collection).InsertOneAsync(Session, entity, null, cancellation);
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
        public Task<BulkWriteResult<T>> InsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellation = default, string? collectionName = null, IMongoCollection<T>? collection = null) where T : IEntity
        {
            SetModifiedByMultiple(entities);
            foreach (var ent in entities) OnBeforeSave(ent);

            var models = entities.Select(ent =>
            {
                PrepAndCheckIfInsert(ent);
                return new InsertOneModel<T>(ent);
            }).ToList();

            return Session == null
                   ? Collection(collectionName, collection).BulkWriteAsync(models, _unOrdBlkOpts, cancellation)
                   : Collection(collectionName, collection).BulkWriteAsync(Session, models, _unOrdBlkOpts, cancellation);
        }
    }
}
