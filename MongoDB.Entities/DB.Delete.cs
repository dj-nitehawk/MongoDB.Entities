using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DB
    {
        private static DeleteResult DeleteCascading<T>(IEnumerable<string> IDs, IClientSessionHandle session = null) where T : IEntity
        {
            DeleteCascadingPrep<T>(
                out IMongoDatabase db,
                out ListCollectionNamesOptions options);

            foreach (var cName in db.ListCollectionNames(options).ToList())
            {
                if (session == null) db.GetCollection<JoinRecord>(cName).DeleteMany(r => IDs.Contains(r.ChildID) || IDs.Contains(r.ParentID));
                else db.GetCollection<JoinRecord>(cName).DeleteMany(session, r => IDs.Contains(r.ChildID) || IDs.Contains(r.ParentID), null);
            }

            var delRes =
                    session == null
                    ? Collection<T>().DeleteMany(x => IDs.Contains(x.ID))
                    : Collection<T>().DeleteMany(session, x => IDs.Contains(x.ID), null);

            if (typeof(T).BaseType == typeof(FileEntity))
            {
                if (session == null) db.GetCollection<FileChunk>(CollectionName<FileChunk>()).DeleteMany(x => IDs.Contains(x.FileID));
                else db.GetCollection<FileChunk>(CollectionName<FileChunk>()).DeleteMany(session, x => IDs.Contains(x.FileID), null);
            }

            return delRes;
        }

        private static async Task<DeleteResult> DeleteCascadingAsync<T>(IEnumerable<string> IDs, IClientSessionHandle session = null) where T : IEntity
        {
            // note: cancellation should not be enabled because multiple collections are involved 
            //       and premature cancellation could cause data inconsistencies.

            DeleteCascadingPrep<T>(
                out IMongoDatabase db,
                out ListCollectionNamesOptions options);

            var tasks = new HashSet<Task>();

            foreach (var cName in await db.ListCollectionNames(options).ToListAsync().ConfigureAwait(false))
            {
                tasks.Add(
                    session == null
                    ? db.GetCollection<JoinRecord>(cName).DeleteManyAsync(r => IDs.Contains(r.ChildID) || IDs.Contains(r.ParentID))
                    : db.GetCollection<JoinRecord>(cName).DeleteManyAsync(session, r => IDs.Contains(r.ChildID) || IDs.Contains(r.ParentID), null));
            }

            var delResTask =
                    session == null
                    ? Collection<T>().DeleteManyAsync(x => IDs.Contains(x.ID))
                    : Collection<T>().DeleteManyAsync(session, x => IDs.Contains(x.ID), null);

            tasks.Add(delResTask);

            if (typeof(T).BaseType == typeof(FileEntity))
            {
                tasks.Add(
                    session == null
                    ? db.GetCollection<FileChunk>(CollectionName<FileChunk>()).DeleteManyAsync(x => IDs.Contains(x.FileID))
                    : db.GetCollection<FileChunk>(CollectionName<FileChunk>()).DeleteManyAsync(session, x => IDs.Contains(x.FileID), null));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return await delResTask.ConfigureAwait(false);
        }

        private static void DeleteCascadingPrep<T>(out IMongoDatabase db, out ListCollectionNamesOptions options) where T : IEntity
        {
            db = GetDatabase<T>();
            options = new ListCollectionNamesOptions
            {
                Filter = "{$and:[{name:/~/},{name:/" + CollectionName<T>() + "/}]}"
            };
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="ID">The Id of the entity to delete</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static DeleteResult Delete<T>(string ID, IClientSessionHandle session = null) where T : IEntity
        {
            return DeleteCascading<T>(new[] { ID }, session);
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="ID">The Id of the entity to delete</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public DeleteResult Delete<T>(string ID, IClientSessionHandle session = null, bool _ = false) where T : IEntity
        {
            return Delete<T>(ID, session);
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="ID">The Id of the entity to delete</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static Task<DeleteResult> DeleteAsync<T>(string ID, IClientSessionHandle session = null) where T : IEntity
        {
            return DeleteCascadingAsync<T>(new[] { ID }, session);
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="ID">The Id of the entity to delete</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public Task<DeleteResult> DeleteAsync<T>(string ID, IClientSessionHandle session = null, bool _ = false) where T : IEntity
        {
            return DeleteCascadingAsync<T>(new[] { ID }, session);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static DeleteResult Delete<T>(Expression<Func<T, bool>> expression, IClientSessionHandle session = null) where T : IEntity
        {
            return DeleteCascading<T>(FindIDsCommand(expression).Execute(), session);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public DeleteResult Delete<T>(Expression<Func<T, bool>> expression, IClientSessionHandle session = null, bool _ = false) where T : IEntity
        {
            return Delete(expression, session);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static async Task<DeleteResult> DeleteAsync<T>(Expression<Func<T, bool>> expression, IClientSessionHandle session = null) where T : IEntity
        {
            return await DeleteCascadingAsync<T>(
                await FindIDsCommand(expression).ExecuteAsync().ConfigureAwait(false),
                session).ConfigureAwait(false);
        }

        private static Find<T, string> FindIDsCommand<T>(Expression<Func<T, bool>> expression) where T : IEntity
        {
            return Find<T, string>()
                    .Match(expression)
                    .Project(e => e.ID);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public Task<DeleteResult> DeleteAsync<T>(Expression<Func<T, bool>> expression, IClientSessionHandle session = null, bool _ = false) where T : IEntity
        {
            return DeleteAsync(expression, session);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static DeleteResult Delete<T>(IEnumerable<string> IDs, IClientSessionHandle session = null) where T : IEntity
        {
            return DeleteCascading<T>(IDs, session);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public DeleteResult Delete<T>(IEnumerable<string> IDs, IClientSessionHandle session = null, bool _ = false) where T : IEntity
        {
            return Delete<T>(IDs, session);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public static Task<DeleteResult> DeleteAsync<T>(IEnumerable<string> IDs, IClientSessionHandle session = null) where T : IEntity
        {
            return DeleteCascadingAsync<T>(IDs, session);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        public Task<DeleteResult> DeleteAsync<T>(IEnumerable<string> IDs, IClientSessionHandle session = null, bool _ = false) where T : IEntity
        {
            return DeleteCascadingAsync<T>(IDs, session);
        }
    }
}
