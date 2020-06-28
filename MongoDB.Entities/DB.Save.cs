using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DB
    {
        private static readonly BulkWriteOptions unOrdBlkOpts = new BulkWriteOptions { IsOrdered = false };

        /// <summary>
        /// Persists an entity to MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public static ReplaceOneResult Save<T>(T entity, IClientSessionHandle session = null) where T : IEntity, new()
        {
            return Run.Sync(() => SaveAsync(entity, session));
        }

        /// <summary>
        /// Persists an entity to MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public ReplaceOneResult Save<T>(T entity, IClientSessionHandle session = null, string db = null) where T : IEntity, new()
        {
            return Run.Sync(() => DB.SaveAsync(entity, session));
        }

        /// <summary>
        /// Persists an entity to MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">And optional cancellation token</param>
        public static Task<ReplaceOneResult> SaveAsync<T>(T entity, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity, new()
        {
            if (string.IsNullOrEmpty(entity.ID)) entity.ID = ObjectId.GenerateNewId().ToString();
            entity.ModifiedOn = DateTime.UtcNow;

            return session == null
                   ? Collection<T>().ReplaceOneAsync(x => x.ID.Equals(entity.ID), entity, new ReplaceOptions { IsUpsert = true }, cancellation)
                   : Collection<T>().ReplaceOneAsync(session, x => x.ID.Equals(entity.ID), entity, new ReplaceOptions { IsUpsert = true }, cancellation);
        }

        /// <summary>
        /// Persists an entity to MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">And optional cancellation token</param>
        public Task<ReplaceOneResult> SaveAsync<T>(T entity, IClientSessionHandle session = null, CancellationToken cancellation = default, string db = null) where T : IEntity
        {
            return SaveAsync(entity, session, cancellation);
        }

        /// <summary>
        /// Persists multiple entities to MongoDB in a single bulk operation
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public static BulkWriteResult<T> Save<T>(IEnumerable<T> entities, IClientSessionHandle session = null) where T : IEntity, new()
        {
            return Run.Sync(() => SaveAsync(entities, session));
        }

        /// <summary>
        /// Persists multiple entities to MongoDB in a single bulk operation
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public BulkWriteResult<T> Save<T>(IEnumerable<T> entities, IClientSessionHandle session = null, string db = null) where T : IEntity, new()
        {
            return Run.Sync(() => DB.SaveAsync(entities, session));
        }

        /// <summary>
        /// Persists multiple entities to MongoDB in a single bulk operation
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">And optional cancellation token</param>
        public static Task<BulkWriteResult<T>> SaveAsync<T>(IEnumerable<T> entities, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity, new()
        {
            var models = new List<WriteModel<T>>();
            foreach (var ent in entities)
            {
                if (string.IsNullOrEmpty(ent.ID)) ent.ID = ObjectId.GenerateNewId().ToString();
                ent.ModifiedOn = DateTime.UtcNow;

                var upsert = new ReplaceOneModel<T>(
                        filter: Builders<T>.Filter.Eq(e => e.ID, ent.ID),
                        replacement: ent)
                { IsUpsert = true };
                models.Add(upsert);
            }

            return session == null
                   ? Collection<T>().BulkWriteAsync(models, unOrdBlkOpts, cancellation)
                   : Collection<T>().BulkWriteAsync(session, models, unOrdBlkOpts, cancellation);
        }

        /// <summary>
        /// Persists multiple entities to MongoDB in a single bulk operation
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">And optional cancellation token</param>
        public Task<BulkWriteResult<T>> SaveAsync<T>(IEnumerable<T> entities, IClientSessionHandle session = null, CancellationToken cancellation = default, string db = null) where T : IEntity
        {
            return SaveAsync(entities, session, cancellation);
        }

        /// <summary>
        /// Saves an entity while preserving some property values in the database. 
        /// The properties to be preserved can be specified with a 'New' expression or using the [Preserve] attribute.
        /// <para>TIP: The 'New' expression should specify only root level properties.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="preservation">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public static UpdateResult SavePreserving<T>(T entity, Expression<Func<T, object>> preservation = null, IClientSessionHandle session = null) where T : IEntity, new()
        {
            return Run.Sync(() => SavePreservingAsync(entity, preservation, session));
        }

        /// <summary>
        /// Saves an entity while preserving some property values in the database. 
        /// The properties to be preserved can be specified with a 'New' expression or using the [Preserve] attribute.
        /// <para>TIP: The 'New' expression should specify only root level properties.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="preservation">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public UpdateResult SavePreserving<T>(T entity, Expression<Func<T, object>> preservation = null, IClientSessionHandle session = null, string db = null) where T : IEntity, new()
        {
            return Run.Sync(() => DB.SavePreservingAsync(entity, preservation, session));
        }

        /// <summary>
        /// Saves an entity while preserving some property values in the database. 
        /// The properties to be preserved can be specified with a 'New' expression or using the [Preserve] attribute.
        /// <para>TIP: The 'New' expression should specify only root level properties.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="preservation">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<UpdateResult> SavePreservingAsync<T>(T entity, Expression<Func<T, object>> preservation = null, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity, new()
        {
            entity.ThrowIfUnsaved();

            var props = entity.GetType().GetProperties()
                .Where(p =>
                       p.PropertyType.Name != ManyBase.PropType &&
                       !p.IsDefined(typeof(BsonIdAttribute), false) &&
                       !p.IsDefined(typeof(BsonIgnoreAttribute), false) &&
                       !(p.IsDefined(typeof(BsonIgnoreIfDefaultAttribute), false) && p.GetValue(entity) == default) &&
                       !(p.IsDefined(typeof(BsonIgnoreIfNullAttribute), false) && p.GetValue(entity) == null));

            string[] excludes;

            if (preservation == null)
            {
                excludes = props.Where(p => p.IsDefined(typeof(PreserveAttribute), false))
                                .Select(p => p.Name)
                                .ToArray();

                if (excludes.Length == 0)
                    throw new ArgumentException("There were no properties decorated with the [Preserve] attribute!");
            }
            else
            {
                excludes = (preservation.Body as NewExpression)?.Arguments
                    .Select(a => a.ToString().Split('.')[1])
                    .ToArray();

                if (excludes.Length == 0)
                    throw new ArgumentException("Unable to get any properties from the preservation expression!");
            }

            props = props.Where(p => !excludes.Contains(p.Name));

            if (!props.Any())
                throw new ArgumentException("At least one property must be not preserved!");

            var defs = new Collection<UpdateDefinition<T>>();

            foreach (var p in props)
            {
                if (p.Name == nameof(entity.ModifiedOn))
                {
                    defs.Add(Builders<T>.Update.CurrentDate(nameof(entity.ModifiedOn)));
                }
                else
                {
                    defs.Add(Builders<T>.Update.Set(p.Name, p.GetValue(entity)));
                }
            }

            return
                session == null
                ? Collection<T>().UpdateOneAsync(e => e.ID == entity.ID, Builders<T>.Update.Combine(defs), null, cancellation)
                : Collection<T>().UpdateOneAsync(session, e => e.ID == entity.ID, Builders<T>.Update.Combine(defs), null, cancellation);
        }

        /// <summary>
        /// Saves an entity while preserving some property values in the database. 
        /// The properties to be preserved can be specified with a 'New' expression or using the [Preserve] attribute.
        /// <para>TIP: The 'New' expression should specify only root level properties.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="preservation">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<UpdateResult> SavePreservingAsync<T>(T entity, Expression<Func<T, object>> preservation = null, IClientSessionHandle session = null, CancellationToken cancellation = default, string db = null) where T : IEntity
        {
            return SavePreservingAsync(entity, preservation, session, cancellation);
        }
    }
}
