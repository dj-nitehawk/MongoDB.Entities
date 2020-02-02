using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DB
    {
        /// <summary>
        /// Persists an entity to MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public static void Save<T>(T entity, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            Run.Sync(() => SaveAsync(entity, session, db));
        }

        /// <summary>
        /// Persists an entity to MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public void Save<T>(T entity, IClientSessionHandle session = null) where T : IEntity
        {
            Run.Sync(() => SaveAsync(entity, session, DbName));
        }

        /// <summary>
        /// Persists an entity to MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public static Task SaveAsync<T>(T entity, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            if (string.IsNullOrEmpty(entity.ID)) entity.ID = ObjectId.GenerateNewId().ToString();
            entity.ModifiedOn = DateTime.UtcNow;

            return session == null
                   ? Collection<T>(db).ReplaceOneAsync(x => x.ID.Equals(entity.ID), entity, new ReplaceOptions { IsUpsert = true })
                   : Collection<T>(db).ReplaceOneAsync(session, x => x.ID.Equals(entity.ID), entity, new ReplaceOptions { IsUpsert = true });
        }

        /// <summary>
        /// Persists an entity to MongoDB
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public Task SaveAsync<T>(T entity, IClientSessionHandle session = null) where T : IEntity
        {
            return SaveAsync(entity, session, DbName);
        }

        /// <summary>
        /// Persists multiple entities to MongoDB in a single bulk operation
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public static void Save<T>(IEnumerable<T> entities, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            Run.Sync(() => SaveAsync(entities, session, db));
        }

        /// <summary>
        /// Persists multiple entities to MongoDB in a single bulk operation
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public void Save<T>(IEnumerable<T> entities, IClientSessionHandle session = null) where T : IEntity
        {
            Run.Sync(() => SaveAsync(entities, session, DbName));
        }

        /// <summary>
        /// Persists multiple entities to MongoDB in a single bulk operation
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public static Task SaveAsync<T>(IEnumerable<T> entities, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            var models = new Collection<WriteModel<T>>();
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
                   ? Collection<T>(db).BulkWriteAsync(models)
                   : Collection<T>(db).BulkWriteAsync(session, models);
        }
    }
}
