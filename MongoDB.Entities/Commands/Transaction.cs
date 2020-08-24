using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents a transaction used to carry out inter-related write operations.
    /// <para>TIP: Remember to always call .Dispose() after use or enclose in a 'Using' statement.</para>
    /// <para>IMPORTANT: Use the methods on this transaction to perform operations and not the methods on the DB class.</para>
    /// </summary>
    public class Transaction : IDisposable
    {
        public IClientSessionHandle Session { get; }
        private readonly IMongoClient client;

        /// <summary>
        /// Instantiates and begins a transaction.
        /// </summary>
        /// <param name="database">The name of the database to use for this transaction. default db is used if not specified</param>
        /// <param name="options">Client session options for this transaction</param>
        public Transaction(string database = default, ClientSessionOptions options = null)
        {
            client = DB.Database(database).Client;
            Session = client.StartSession(options);
            Session.StartTransaction();
        }

        /// <summary>
        /// Commits a tranaction to MongoDB
        /// </summary>
        public void Commit() => Session.CommitTransaction();

        /// <summary>
        /// Commits a tranaction to MongoDB
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task CommitAsync(CancellationToken cancellation = default) => Session.CommitTransactionAsync(cancellation);

        /// <summary>
        /// Aborts and rolls back a tranaction
        /// </summary>
        public void Abort() => Session.AbortTransaction();

        /// <summary>
        /// Aborts and rolls back a tranaction
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task AbortAsync(CancellationToken cancellation = default) => Session.AbortTransactionAsync(cancellation);

        public Update<T> Update<T>() where T : IEntity
        {
            return new Update<T>(Session);
        }

        public UpdateAndGet<T> UpdateAndGet<T>() where T : IEntity
        {
            return new UpdateAndGet<T>(Session);
        }

        public UpdateAndGet<T, TProjection> UpdateAndGet<T, TProjection>() where T : IEntity
        {
            return new UpdateAndGet<T, TProjection>(Session);
        }

        public Find<T> Find<T>() where T : IEntity
        {
            return new Find<T>(Session);
        }

        public Find<T, TProjection> Find<T, TProjection>() where T : IEntity
        {
            return new Find<T, TProjection>(Session);
        }

        public IAggregateFluent<T> Fluent<T>(AggregateOptions options = null) where T : IEntity
        {
            return DB.Fluent<T>(options, Session);
        }

        public IMongoQueryable<T> Queryable<T>(AggregateOptions options = null) where T : IEntity
        {
            return DB.Queryable<T>(options, Session);
        }

        public IAsyncCursor<TResult> Aggregate<T, TResult>(Template<T, TResult> template, AggregateOptions options = null) where T : IEntity
        {
            return DB.Aggregate(template, options, Session);
        }

        public Task<IAsyncCursor<TResult>> AggregateAsync<T, TResult>(Template<T, TResult> template, AggregateOptions options = null) where T : IEntity
        {
            return DB.AggregateAsync(template, options, Session);
        }

        public IAggregateFluent<T> GeoNear<T>(Coordinates2D NearCoordinates, Expression<Func<T, object>> DistanceField, bool Spherical = true, int? MaxDistance = null, int? MinDistance = null, int? Limit = null, BsonDocument Query = null, int? DistanceMultiplier = null, Expression<Func<T, object>> IncludeLocations = null, string IndexKey = null, AggregateOptions options = null) where T : IEntity
        {
            return DB.FluentGeoNear(NearCoordinates, DistanceField, Spherical, MaxDistance, MinDistance, Limit, Query, DistanceMultiplier, IncludeLocations, IndexKey, options, Session);
        }

        public ReplaceOneResult Save<T>(T entity) where T : IEntity
        {
            return DB.Save(entity, Session);
        }

        public Task<ReplaceOneResult> SaveAsync<T>(T entity, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.SaveAsync(entity, Session, cancellation);
        }

        public BulkWriteResult<T> Save<T>(IEnumerable<T> entities) where T : IEntity
        {
            return DB.Save(entities, Session);
        }

        public Task<BulkWriteResult<T>> SaveAsync<T>(IEnumerable<T> entities, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.SaveAsync(entities, Session, cancellation);
        }

        public UpdateResult SavePreserving<T>(T entity, Expression<Func<T, object>> preservation = null) where T : IEntity
        {
            return DB.SavePreserving(entity, preservation, Session);
        }

        public Task<UpdateResult> SavePreservingAsync<T>(T entity, Expression<Func<T, object>> preservation = null, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.SavePreservingAsync(entity, preservation, Session, cancellation);
        }

        public DeleteResult Delete<T>(string ID) where T : IEntity
        {
            return DB.Delete<T>(ID, Session);
        }

        public Task<DeleteResult> DeleteAsync<T>(string ID) where T : IEntity
        {
            return DB.DeleteAsync<T>(ID, Session);
        }

        public DeleteResult Delete<T>(Expression<Func<T, bool>> expression) where T : IEntity
        {
            return DB.Delete(expression, Session);
        }

        public Task<DeleteResult> DeleteAsync<T>(Expression<Func<T, bool>> expression) where T : IEntity
        {
            return DB.DeleteAsync(expression, Session);
        }

        public DeleteResult Delete<T>(IEnumerable<string> IDs) where T : IEntity
        {
            return DB.Delete<T>(IDs, Session);
        }

        public Task<DeleteResult> DeleteAsync<T>(IEnumerable<string> IDs) where T : IEntity
        {
            return DB.DeleteAsync<T>(IDs, Session);
        }

        public IAggregateFluent<T> FluentTextSearch<T>(Search searchType, string searchTerm, bool caseSensitive = false, bool diacriticSensitive = false, string language = null, AggregateOptions options = null) where T : IEntity
        {
            return DB.FluentTextSearch<T>(searchType, searchTerm, caseSensitive, diacriticSensitive, language, options, Session);
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Session.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion        
    }
}
