using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
        public static ReplaceOneResult Save<T>(T entity, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            return Run.Sync(() => SaveAsync(entity, session, db));
        }

        /// <summary>
        /// Persists an entity to MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public ReplaceOneResult Save<T>(T entity, IClientSessionHandle session = null) where T : IEntity
        {
            return Run.Sync(() => SaveAsync(entity, session, DbName));
        }

        /// <summary>
        /// Persists an entity to MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">And optional cancellation token</param>
        public static Task<ReplaceOneResult> SaveAsync<T>(T entity, IClientSessionHandle session = null, string db = null, CancellationToken cancellation = default) where T : IEntity
        {
            if (string.IsNullOrEmpty(entity.ID)) entity.ID = ObjectId.GenerateNewId().ToString();
            entity.ModifiedOn = DateTime.UtcNow;

            return session == null
                   ? Collection<T>(db).ReplaceOneAsync(x => x.ID.Equals(entity.ID), entity, new ReplaceOptions { IsUpsert = true }, cancellation)
                   : Collection<T>(db).ReplaceOneAsync(session, x => x.ID.Equals(entity.ID), entity, new ReplaceOptions { IsUpsert = true }, cancellation);
        }

        /// <summary>
        /// Persists an entity to MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">And optional cancellation token</param>
        public Task<ReplaceOneResult> SaveAsync<T>(T entity, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return SaveAsync(entity, session, DbName, cancellation);
        }

        /// <summary>
        /// Persists multiple entities to MongoDB in a single bulk operation
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public static BulkWriteResult<T> Save<T>(IEnumerable<T> entities, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            return Run.Sync(() => SaveAsync(entities, session, db));
        }

        /// <summary>
        /// Persists multiple entities to MongoDB in a single bulk operation
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public BulkWriteResult<T> Save<T>(IEnumerable<T> entities, IClientSessionHandle session = null) where T : IEntity
        {
            return Run.Sync(() => SaveAsync(entities, session, DbName));
        }

        /// <summary>
        /// Persists multiple entities to MongoDB in a single bulk operation
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">And optional cancellation token</param>
        public static Task<BulkWriteResult<T>> SaveAsync<T>(IEnumerable<T> entities, IClientSessionHandle session = null, string db = null, CancellationToken cancellation = default) where T : IEntity
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
                   ? Collection<T>(db).BulkWriteAsync(models, unOrdBlkOpts, cancellation)
                   : Collection<T>(db).BulkWriteAsync(session, models, unOrdBlkOpts, cancellation);
        }

        /// <summary>
        /// Persists multiple entities to MongoDB in a single bulk operation
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">And optional cancellation token</param>
        public Task<BulkWriteResult<T>> SaveAsync<T>(IEnumerable<T> entities, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return SaveAsync(entities, session, DbName, cancellation);
        }

        /// <summary>
        /// Saves an entity while preserving some property values in the database. 
        /// The properties to be preserved can be specified with a 'New' expression.
        /// <para>TIP: The 'New' expression should specify only root level properties.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="preservation">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public static ReplaceOneResult SavePreserving<T>(T entity, Expression<Func<T, object>> preservation, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            return Run.Sync(() => SavePreservingAsync(entity, preservation, session, db));
        }

        /// <summary>
        /// Saves an entity while preserving some property values in the database. 
        /// The properties to be preserved can be specified with a 'New' expression.
        /// <para>TIP: The 'New' expression should specify only root level properties.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="preservation">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static async Task<ReplaceOneResult> SavePreservingAsync<T>(T entity, Expression<Func<T, object>> preservation, IClientSessionHandle session = null, string db = null, CancellationToken cancellation = default) where T : IEntity
        {
            entity.ThrowIfUnsaved();

            var args = (preservation.Body as NewExpression)?.Members.Select(m => m as PropertyInfo);

            var props = (preservation.Body as NewExpression)?.Arguments
                .Select(a => a.ToString()
                              .Split('.')
                              [1])
                .ToArray();

            if (props.Length == 0)
                throw new ArgumentException("Unable to get any properties from the preservation expression!");

            var filter = Builders<T>.Filter.Eq(e => e.ID, entity.ID);
            var options = new FindOptions<T, object> { Projection = Builders<T>.Projection.Expression(preservation) };
            var result = (await (
                    session == null
                    ? Collection<T>(db).FindAsync(filter, options, cancellation)
                    : Collection<T>(db).FindAsync(session, filter, options, cancellation)
                    ))
                    .ToList()
                    .SingleOrDefault();

            if (result != null)
            {
                var fromType = result.GetType();
                var toType = entity.GetType();

                foreach (var prop in props)
                {
                    toType.GetProperty(prop)?.SetValue(
                        entity,
                        fromType.GetProperty(prop)?.GetValue(result));
                }
            }
            else
            {
                throw new ArgumentException("Unable to locate entity in database for preservation purposes!");
            }

            return await SaveAsync(entity, session, db, cancellation);
        }

        /// <summary>
        /// Saves an entity while preserving some property values in the database. 
        /// The properties to be preserved can be specified with a 'New' expression.
        /// <para>TIP: The 'New' expression should specify only root level properties.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="preservation">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public ReplaceOneResult SavePreserving<T>(T entity, Expression<Func<T, object>> preservation, IClientSessionHandle session = null) where T : IEntity
        {
            return Run.Sync(() => SavePreservingAsync(entity, preservation, session, DbName));
        }

        /// <summary>
        /// Saves an entity while preserving some property values in the database. 
        /// The properties to be preserved can be specified with a 'New' expression.
        /// <para>TIP: The 'New' expression should specify only root level properties.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="preservation">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<ReplaceOneResult> SavePreservingAsync<T>(T entity, Expression<Func<T, object>> preservation, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return SavePreservingAsync(entity, preservation, session, DbName, cancellation);
        }
    }
}
