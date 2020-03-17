using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DB
    {
        private static async Task<DeleteResult> DeleteCascadingAsync<T>(IEnumerable<string> IDs, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            var joinCollections = (await GetDatabase(db).ListCollectionNames().ToListAsync())
                                                  .Where(c =>
                                                         c.Contains("~") &&
                                                         c.Contains(GetCollectionName<T>()));
            var tasks = new HashSet<Task>();

            foreach (var cName in joinCollections)
            {
                tasks.Add(session == null
                          ? GetDatabase(db).GetCollection<JoinRecord>(cName).DeleteManyAsync(r => IDs.Contains(r.ChildID) || IDs.Contains(r.ParentID))
                          : GetDatabase(db).GetCollection<JoinRecord>(cName).DeleteManyAsync(session, r => IDs.Contains(r.ChildID) || IDs.Contains(r.ParentID)));
            }

            var delRes =
                    session == null
                    ? Collection<T>(db).DeleteManyAsync(x => IDs.Contains(x.ID))
                    : Collection<T>(db).DeleteManyAsync(session, x => IDs.Contains(x.ID));

            tasks.Add(delRes);

            if (typeof(T).BaseType == typeof(FileEntity))
            {
                tasks.Add(session == null
                    ? Collection<FileChunk>(db).DeleteManyAsync(x => IDs.Contains(x.FileID))
                    : Collection<FileChunk>(db).DeleteManyAsync(session, x => IDs.Contains(x.FileID)));
            }

            await Task.WhenAll(tasks);

            return await delRes;
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="ID">The Id of the entity to delete</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static DeleteResult Delete<T>(string ID, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            return Run.Sync(() => DeleteAsync<T>(ID, session, db));
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="ID">The Id of the entity to delete</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public DeleteResult Delete<T>(string ID, IClientSessionHandle session = null) where T : IEntity
        {
            return Run.Sync(() => DeleteAsync<T>(ID, session, DbName));
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="ID">The Id of the entity to delete</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static Task<DeleteResult> DeleteAsync<T>(string ID, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            return DeleteCascadingAsync<T>(new[] { ID }, session, db);
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="ID">The Id of the entity to delete</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public Task<DeleteResult> DeleteAsync<T>(string ID, IClientSessionHandle session = null) where T : IEntity
        {
            return DeleteCascadingAsync<T>(new[] { ID }, session, DbName);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a batch</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static DeleteResult Delete<T>(Expression<Func<T, bool>> expression, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            return Run.Sync(() => DeleteAsync(expression, session, db));
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a batch</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public DeleteResult Delete<T>(Expression<Func<T, bool>> expression, IClientSessionHandle session = null) where T : IEntity
        {
            return Run.Sync(() => DeleteAsync(expression, session, DbName));
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a batch</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static async Task<DeleteResult> DeleteAsync<T>(Expression<Func<T, bool>> expression, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            var IDs = await Queryable<T>(db: db)
                              .Where(expression)
                              .Select(e => e.ID)
                              .ToListAsync();

            return await DeleteCascadingAsync<T>(IDs, session, db);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a batch</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public Task<DeleteResult> DeleteAsync<T>(Expression<Func<T, bool>> expression, IClientSessionHandle session = null) where T : IEntity
        {
            return DeleteAsync(expression, session, DbName);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a batch</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static DeleteResult Delete<T>(IEnumerable<string> IDs, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            return Run.Sync(() => DeleteAsync<T>(IDs, session, db));
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a batch</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public DeleteResult Delete<T>(IEnumerable<string> IDs, IClientSessionHandle session = null) where T : IEntity
        {
            return Run.Sync(() => DeleteAsync<T>(IDs, session, DbName));
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a batch</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static Task<DeleteResult> DeleteAsync<T>(IEnumerable<string> IDs, IClientSessionHandle session = null, string db = null) where T : IEntity
        {
            return DeleteCascadingAsync<T>(IDs, session, db);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a batch</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public Task<DeleteResult> DeleteAsync<T>(IEnumerable<string> IDs, IClientSessionHandle session = null) where T : IEntity
        {
            return DeleteCascadingAsync<T>(IDs, session, DbName);
        }
    }
}
