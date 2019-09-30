using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
        private string db = null;

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
        public async void CommitAsync() => await Session.CommitTransactionAsync();

        /// <summary>
        /// Aborts and rolls back a tranaction
        /// </summary>
        public void Abort() => Session.AbortTransaction();

        /// <summary>
        /// Aborts and rolls back a tranaction
        /// </summary>
        public async void AbortAsync() => await Session.AbortTransactionAsync();

        public Update<T> Update<T>() where T : Entity
        {
            return new Update<T>(Session, db);
        }

        public Find<T> Find<T>() where T : Entity
        {
            return new Find<T>(Session, db);
        }

        public Find<T, TProjection> Find<T, TProjection>() where T : Entity
        {
            return new Find<T, TProjection>(Session, db);
        }

        public IAggregateFluent<T> Fluent<T>(AggregateOptions options = null)
        {
            return DB.Fluent<T>(options, Session, db);
        }

        public IAggregateFluent<T> GeoNear<T>(Coordinates2D NearCoordinates, Expression<Func<T, object>> DistanceField, bool Spherical = true, int? MaxDistance = null, int? MinDistance = null, int? Limit = null, BsonDocument Query = null, int? DistanceMultiplier = null, Expression<Func<T, object>> IncludeLocations = null, string IndexKey = null, AggregateOptions options = null) where T : Entity
        {
            return (new GeoNear<T>
            {
                near = NearCoordinates,
                distanceField = DistanceField.FullPath(),
                spherical = Spherical,
                maxDistance = MaxDistance,
                minDistance = MinDistance,
                query = Query,
                distanceMultiplier = DistanceMultiplier,
                limit = Limit,
                includeLocs = IncludeLocations.FullPath(),
                key = IndexKey,
            })
            .ToFluent(options, Session, db);
        }

        public void Save<T>(T entity) where T : Entity
        {
            SaveAsync(entity).GetAwaiter().GetResult();
        }

        public async Task SaveAsync<T>(T entity) where T : Entity
        {
            await DB.SaveAsync(entity, Session, db);
        }

        public void Save<T>(IEnumerable<T> entities) where T : Entity
        {
            SaveAsync(entities).GetAwaiter().GetResult();
        }

        public async Task SaveAsync<T>(IEnumerable<T> entities) where T : Entity
        {
            await DB.SaveAsync<T>(entities, Session, db);
        }

        public void Delete<T>(string ID) where T : Entity
        {
            DeleteAsync<T>(ID).GetAwaiter().GetResult();
        }

        public async Task DeleteAsync<T>(string ID) where T : Entity
        {
            await DB.DeleteAsync<T>(ID, Session, db);
        }

        public void Delete<T>(Expression<Func<T, bool>> expression) where T : Entity
        {
            DeleteAsync(expression).GetAwaiter().GetResult();
        }

        public async Task DeleteAsync<T>(Expression<Func<T, bool>> expression) where T : Entity
        {
            await DB.DeleteAsync(expression, Session, db);
        }

        public void Delete<T>(IEnumerable<string> IDs) where T : Entity
        {
            DeleteAsync<T>(IDs).GetAwaiter().GetResult();
        }

        public async Task DeleteAsync<T>(IEnumerable<string> IDs) where T : Entity
        {
            await DB.DeleteAsync<T>(IDs, Session, db);
        }

        public List<T> SearchText<T>(string searchTerm, bool caseSensitive = false, FindOptions<T, T> options = null)
        {
            return SearchTextAsync(searchTerm, caseSensitive, options).GetAwaiter().GetResult();
        }

        public async Task<List<T>> SearchTextAsync<T>(string searchTerm, bool caseSensitive = false, FindOptions<T, T> options = null)
        {
            return await DB.SearchTextAsync(searchTerm, caseSensitive, options, Session, db);
        }

        public IAggregateFluent<T> SearchTextFluent<T>(string searchTerm, bool caseSensitive = false, AggregateOptions options = null)
        {
            return DB.SearchTextFluent<T>(searchTerm, caseSensitive, options, Session, db);
        }

        /// <summary>
        /// Ends the transaction and disposes the session.
        /// </summary>
        public void Dispose()
        {
            Session.Dispose();
        }
    }
}
