using MongoDB.Driver;
using MongoDB.Entities.Core;
using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public partial class DB
    {
        internal static Task<UpdateResult> UpdateAsync<T>(FilterDefinition<T> filter, UpdateDefinition<T> definition, UpdateOptions options, IClientSessionHandle session = null, string db = null, CancellationToken cancellation = default)
        {
            return session == null
                   ? Collection<T>(db).UpdateManyAsync(filter, definition, options, cancellation)
                   : Collection<T>(db).UpdateManyAsync(session, filter, definition, options, cancellation);
        }

        //note: only filter by lambda expression is available due to projection cannot be achieved with filterdefinition using official driver
        internal static Task<TProjection> UpdateAndGetAsync<T, TProjection>(Expression<Func<T, bool>> filter, UpdateDefinition<T> definition, FindOneAndUpdateOptions<T, TProjection> options, IClientSessionHandle session = null, string db = null, CancellationToken cancellation = default)
        {
            return session == null
                ? Collection<T>(db).FindOneAndUpdateAsync<T, TProjection>(filter, definition, options, cancellation)
                : Collection<T>(db).FindOneAndUpdateAsync<T, TProjection>(session, filter, definition, options, cancellation);
        }

        internal static Task<BulkWriteResult<T>> BulkUpdateAsync<T>(Collection<UpdateManyModel<T>> models, IClientSessionHandle session = null, string db = null, CancellationToken cancellation = default)
        {
            return session == null
                   ? Collection<T>(db).BulkWriteAsync(models, null, cancellation)
                   : Collection<T>(db).BulkWriteAsync(session, models, null, cancellation);
        }

        /// <summary>
        /// Represents an update command
        /// <para>TIP: Specify a filter first with the .Match() method. Then set property values with .Modify() and finally call .Execute() to run the command.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static Update<T> Update<T>(string db = null) where T : IEntity
        {
            return new Update<T>(db: db);
        }

        /// <summary>
        /// Represents an update command
        /// <para>TIP: Specify a filter first with the .Match() method. Then set property values with .Modify() and finally call .Execute() to run the command.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public Update<T> Update<T>() where T : IEntity
        {
            return new Update<T>(db: DbName);
        }

        /// <summary>
        /// Update and retrieve the first document that was updated.
        /// <para>TIP: Specify a filter first with the .Match(). Then set property values with .Modify() and finally call .Execute() to run the command.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TProjection">The type to project to</typeparam>
        public static UpdateAndGet<T, TProjection> UpdateAndGet<T, TProjection>(string db = null) where T : IEntity
        {
            return new UpdateAndGet<T, TProjection>(db: db);
        }

        /// <summary>
        /// Update and retrieve the first document that was updated.
        /// <para>TIP: Specify a filter first with the .Match(). Then set property values with .Modify() and finally call .Execute() to run the command.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <typeparam name="TProjection">The type to project to</typeparam>
        public UpdateAndGet<T, TProjection> UpdateAndGet<T, TProjection>() where T : IEntity
        {
            return new UpdateAndGet<T, TProjection>(db: DbName);
        }

        /// <summary>
        /// Update and retrieve the first document that was updated.
        /// <para>TIP: Specify a filter first with the .Match(). Then set property values with .Modify() and finally call .Execute() to run the command.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static UpdateAndGet<T> UpdateAndGet<T>(string db = null) where T : IEntity
        {
            return new UpdateAndGet<T>(db: db);
        }

        /// <summary>
        /// Update and retrieve the first document that was updated.
        /// <para>TIP: Specify a filter first with the .Match(). Then set property values with .Modify() and finally call .Execute() to run the command.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public UpdateAndGet<T> UpdateAndGet<T>() where T : IEntity
        {
            return new UpdateAndGet<T>(db: DbName);
        }
    }
}
