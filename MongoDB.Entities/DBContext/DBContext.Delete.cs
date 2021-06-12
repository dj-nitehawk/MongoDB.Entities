using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DBContext
    {
        /// <summary>
        /// Deletes a single entity from MongoDB
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="ID">The Id of the entity to delete</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<DeleteResult> DeleteAsync<T>(string ID, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.DeleteAsync(
                Logic.MergeWithGlobalFilter(globalFilters, Builders<T>.Filter.Eq(e => e.ID, ID)),
                session,
                cancellation);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name="collation">An optional collation object</param>
        public virtual Task<DeleteResult> DeleteAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellation = default, Collation collation = null) where T : IEntity
        {
            return DB.DeleteAsync(
                Logic.MergeWithGlobalFilter(globalFilters, Builders<T>.Filter.Where(expression)),
                session,
                cancellation,
                collation);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<DeleteResult> DeleteAsync<T>(IEnumerable<string> IDs, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.DeleteAsync(
                Logic.MergeWithGlobalFilter(globalFilters, Builders<T>.Filter.In(e => e.ID, IDs)),
                session,
                cancellation);
        }
    }
}
