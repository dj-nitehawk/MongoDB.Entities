using MongoDB.Driver;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DB
    {
        internal static UpdateResult Update<T>(FilterDefinition<T> filter, UpdateDefinition<T> definition, UpdateOptions options, IClientSessionHandle session = null) where T : IEntity
        {
            return session == null
                   ? Collection<T>().UpdateMany(filter, definition, options)
                   : Collection<T>().UpdateMany(session, filter, definition, options);
        }

        internal static Task<UpdateResult> UpdateAsync<T>(FilterDefinition<T> filter, UpdateDefinition<T> definition, UpdateOptions options, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return session == null
                   ? Collection<T>().UpdateManyAsync(filter, definition, options, cancellation)
                   : Collection<T>().UpdateManyAsync(session, filter, definition, options, cancellation);
        }

        internal static TProjection UpdateAndGet<T, TProjection>(FilterDefinition<T> filter, UpdateDefinition<T> definition, FindOneAndUpdateOptions<T, TProjection> options, IClientSessionHandle session = null) where T : IEntity
        {
            return session == null
                ? Collection<T>().FindOneAndUpdate(filter, definition, options)
                : Collection<T>().FindOneAndUpdate(session, filter, definition, options);
        }

        internal static Task<TProjection> UpdateAndGetAsync<T, TProjection>(FilterDefinition<T> filter, UpdateDefinition<T> definition, FindOneAndUpdateOptions<T, TProjection> options, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return session == null
                ? Collection<T>().FindOneAndUpdateAsync(filter, definition, options, cancellation)
                : Collection<T>().FindOneAndUpdateAsync(session, filter, definition, options, cancellation);
        }

        internal static BulkWriteResult<T> BulkUpdate<T>(Collection<UpdateManyModel<T>> models, IClientSessionHandle session = null) where T : IEntity
        {
            return session == null
                   ? Collection<T>().BulkWrite(models)
                   : Collection<T>().BulkWrite(session, models);
        }

        internal static Task<BulkWriteResult<T>> BulkUpdateAsync<T>(Collection<UpdateManyModel<T>> models, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntity
        {
            return session == null
                   ? Collection<T>().BulkWriteAsync(models, null, cancellation)
                   : Collection<T>().BulkWriteAsync(session, models, null, cancellation);
        }

        /// <summary>
        /// Represents an update command
        /// <para>TIP: Specify a filter first with the .Match() method. Then set property values with .Modify() and finally call .Execute() to run the command.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static Update<T> Update<T>() where T : IEntity
        {
            return new Update<T>();
        }

        /// <summary>
        /// Represents an update command
        /// <para>TIP: Specify a filter first with the .Match() method. Then set property values with .Modify() and finally call .Execute() to run the command.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public Update<T> Update<T>(bool _ = false) where T : IEntity
        {
            return new Update<T>();
        }

        /// <summary>
        /// Update and retrieve the first document that was updated.
        /// <para>TIP: Specify a filter first with the .Match(). Then set property values with .Modify() and finally call .Execute() to run the command.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TProjection">The type to project to</typeparam>
        public static UpdateAndGet<T, TProjection> UpdateAndGet<T, TProjection>() where T : IEntity
        {
            return new UpdateAndGet<T, TProjection>();
        }

        /// <summary>
        /// Update and retrieve the first document that was updated.
        /// <para>TIP: Specify a filter first with the .Match(). Then set property values with .Modify() and finally call .Execute() to run the command.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TProjection">The type to project to</typeparam>
        public UpdateAndGet<T, TProjection> UpdateAndGet<T, TProjection>(bool _ = false) where T : IEntity
        {
            return new UpdateAndGet<T, TProjection>();
        }

        /// <summary>
        /// Update and retrieve the first document that was updated.
        /// <para>TIP: Specify a filter first with the .Match(). Then set property values with .Modify() and finally call .Execute() to run the command.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static UpdateAndGet<T> UpdateAndGet<T>() where T : IEntity
        {
            return new UpdateAndGet<T>();
        }

        /// <summary>
        /// Update and retrieve the first document that was updated.
        /// <para>TIP: Specify a filter first with the .Match(). Then set property values with .Modify() and finally call .Execute() to run the command.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public UpdateAndGet<T> UpdateAndGet<T>(bool _ = false) where T : IEntity
        {
            return new UpdateAndGet<T>();
        }
    }
}
