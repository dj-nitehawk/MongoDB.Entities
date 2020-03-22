using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities.Core;
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
        private readonly string db = null;

        /// <summary>
        /// Instantiates and begins a transaction.
        /// </summary>
        /// <param name="database">The name of the database to use for this transaction</param>
        /// <param name="options">Client session options for this transaction</param>
        public Transaction(string database = null, ClientSessionOptions options = null)
        {
            db = database;
            client = DB.GetClient(db);
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
            return new Update<T>(Session, db);
        }

        public UpdateAndGet<T> UpdateAndGet<T>() where T : IEntity
        {
            return new UpdateAndGet<T>(Session, db);
        }

        public UpdateAndGet<T, TProjection> UpdateAndGet<T, TProjection>() where T : IEntity
        {
            return new UpdateAndGet<T, TProjection>(Session, db);
        }

        public Find<T> Find<T>() where T : IEntity
        {
            return new Find<T>(Session, db);
        }

        public Find<T, TProjection> Find<T, TProjection>() where T : IEntity
        {
            return new Find<T, TProjection>(Session, db);
        }

        public IAggregateFluent<T> Fluent<T>(AggregateOptions options = null)
        {
            return DB.Fluent<T>(options, Session, db);
        }

        public IAsyncCursor<TResult> Aggregate<T, TResult>(Template<T, TResult> template, AggregateOptions options = null) where T : IEntity
        {
            return DB.Aggregate(template, options, Session, db);
        }

        public Task<IAsyncCursor<TResult>> AggregateAsync<T, TResult>(Template<T, TResult> template, AggregateOptions options = null) where T : IEntity
        {
            return DB.AggregateAsync(template, options, Session, db);
        }

        public IAggregateFluent<T> GeoNear<T>(Coordinates2D NearCoordinates, Expression<Func<T, object>> DistanceField, bool Spherical = true, int? MaxDistance = null, int? MinDistance = null, int? Limit = null, BsonDocument Query = null, int? DistanceMultiplier = null, Expression<Func<T, object>> IncludeLocations = null, string IndexKey = null, AggregateOptions options = null) where T : IEntity
        {
            return DB.FluentGeoNear(NearCoordinates, DistanceField, Spherical, MaxDistance, MinDistance, Limit, Query, DistanceMultiplier, IncludeLocations, IndexKey, options, Session, db);
        }

        public ReplaceOneResult Save<T>(T entity) where T : IEntity
        {
            return Run.Sync(() => SaveAsync(entity));
        }

        public Task<ReplaceOneResult> SaveAsync<T>(T entity, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.SaveAsync(entity, Session, db, cancellation);
        }

        public BulkWriteResult<T> Save<T>(IEnumerable<T> entities) where T : IEntity
        {
            return Run.Sync(() => SaveAsync(entities));
        }

        public Task<BulkWriteResult<T>> SaveAsync<T>(IEnumerable<T> entities, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.SaveAsync(entities, Session, db, cancellation);
        }

        public ReplaceOneResult SavePreserving<T>(T entity, Expression<Func<T, object>> preservation) where T : IEntity
        {
            return Run.Sync(() => SavePreservingAsync(entity, preservation));
        }

        public Task<ReplaceOneResult> SavePreservingAsync<T>(T entity, Expression<Func<T, object>> preservation, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.SavePreservingAsync(entity, preservation, Session, db, cancellation);
        }

        public DeleteResult Delete<T>(string ID) where T : IEntity
        {
            return Run.Sync(() => DeleteAsync<T>(ID));
        }

        public Task<DeleteResult> DeleteAsync<T>(string ID, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.DeleteAsync<T>(ID, Session, db);
        }

        public DeleteResult Delete<T>(Expression<Func<T, bool>> expression) where T : IEntity
        {
            return Run.Sync(() => DeleteAsync(expression));
        }

        public Task<DeleteResult> DeleteAsync<T>(Expression<Func<T, bool>> expression) where T : IEntity
        {
            return DB.DeleteAsync(expression, Session, db);
        }

        public DeleteResult Delete<T>(IEnumerable<string> IDs) where T : IEntity
        {
            return Run.Sync(() => DeleteAsync<T>(IDs));
        }

        public Task<DeleteResult> DeleteAsync<T>(IEnumerable<string> IDs) where T : IEntity
        {
            return DB.DeleteAsync<T>(IDs, Session, db);
        }

        public IAggregateFluent<T> FluentTextSearch<T>(Search searchType, string searchTerm, bool caseSensitive = false, bool diacriticSensitive = false, string language = null, AggregateOptions options = null)
        {
            return DB.FluentTextSearch<T>(searchType, searchTerm, caseSensitive, diacriticSensitive, language, options, Session, db);
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
