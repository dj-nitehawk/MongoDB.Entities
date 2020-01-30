using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Core;
using MongoDB.Entities.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DB
    {
        private static async Task DeleteCascadingAsync<T>(IEnumerable<string> IDs, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            var joinCollections = (await GetDB(db).ListCollectionNames().ToListAsync())
                                                  .Where(c =>
                                                         c.Contains("~") &&
                                                         c.Contains(GetCollectionName<T>()));
            var tasks = new HashSet<Task>();

            foreach (var cName in joinCollections)
            {
                tasks.Add(session == null
                          ? GetDB(db).GetCollection<JoinRecord>(cName).DeleteManyAsync(r => IDs.Contains(r.ChildID) || IDs.Contains(r.ParentID))
                          : GetDB(db).GetCollection<JoinRecord>(cName).DeleteManyAsync(session, r => IDs.Contains(r.ChildID) || IDs.Contains(r.ParentID)));
            }

            tasks.Add(session == null
                       ? Collection<T>(db).DeleteManyAsync(x => IDs.Contains(x.ID))
                       : Collection<T>(db).DeleteManyAsync(session, x => IDs.Contains(x.ID)));

            if (typeof(T).BaseType == typeof(FileEntity))
            {
                tasks.Add(session == null
                    ? Collection<FileChunk>(db).DeleteManyAsync(x => IDs.Contains(x.FileID))
                    : Collection<FileChunk>(db).DeleteManyAsync(session, x => IDs.Contains(x.FileID)));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="ID">The Id of the entity to delete</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static void Delete<T>(string ID, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            Run.Sync(() => DeleteAsync<T>(ID, session, db));
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="ID">The Id of the entity to delete</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public void Delete<T>(string ID, IClientSessionHandle session = null) where T : IEntity
        {
            Run.Sync(() => DeleteAsync<T>(ID, session, DbName));
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="ID">The Id of the entity to delete</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static async Task DeleteAsync<T>(string ID, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            await DeleteCascadingAsync<T>(new[] { ID }, session, db);
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="ID">The Id of the entity to delete</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public async Task DeleteAsync<T>(string ID, IClientSessionHandle session = null) where T : IEntity
        {
            await DeleteCascadingAsync<T>(new[] { ID }, session, DbName);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a batch</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static void Delete<T>(Expression<Func<T, bool>> expression, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            Run.Sync(() => DeleteAsync(expression, session, db));
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a batch</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public void Delete<T>(Expression<Func<T, bool>> expression, IClientSessionHandle session = null) where T : IEntity
        {
            Run.Sync(() => DeleteAsync(expression, session, DbName));
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a batch</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static async Task DeleteAsync<T>(Expression<Func<T, bool>> expression, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            var IDs = await Queryable<T>(db: db)
                              .Where(expression)
                              .Select(e => e.ID)
                              .ToListAsync();

            await DeleteCascadingAsync<T>(IDs, session, db);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a batch</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public async Task DeleteAsync<T>(Expression<Func<T, bool>> expression, IClientSessionHandle session = null) where T : IEntity
        {
            await DeleteAsync(expression, session, DbName);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a batch</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static void Delete<T>(IEnumerable<string> IDs, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            Run.Sync(() => DeleteAsync<T>(IDs, session, db));
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a batch</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public void Delete<T>(IEnumerable<string> IDs, IClientSessionHandle session = null) where T : IEntity
        {
            Run.Sync(() => DeleteAsync<T>(IDs, session, DbName));
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a batch</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static async Task DeleteAsync<T>(IEnumerable<string> IDs, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            await DeleteCascadingAsync<T>(IDs, session, db);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a batch</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public async Task DeleteAsync<T>(IEnumerable<string> IDs, IClientSessionHandle session = null) where T : IEntity
        {
            await DeleteCascadingAsync<T>(IDs, session, DbName);
        }
    }
}
