using MongoDB.Driver;
using MongoDB.Entities.Core;
using System.Collections.ObjectModel;
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
    }
}
