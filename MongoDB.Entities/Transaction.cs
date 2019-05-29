using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

//todo: tests

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents a transaction used to carry out inter related write operations.
    /// <para>TIP: Remember to always call .Dispose() after use or enclose in a 'Using' statement.</para>
    /// <para>IMPORTANT: Use the methods on this transaction to perform operations and not the methods on the DB class.</para>
    /// </summary>
    public class Transaction : IDisposable
    {
        public IClientSessionHandle Session { get; }
        private IMongoClient client;

        /// <summary>
        /// Instantiates and begins a transaction.
        /// </summary>
        /// <param name="options">Client session options for this transaction</param>
        public Transaction(ClientSessionOptions options = null)
        {
            client = DB.GetClient();
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
        async public void CommitAsync() => await Session.CommitTransactionAsync();

        /// <summary>
        /// Aborts and rolls back a tranaction
        /// </summary>
        public void Abort() => Session.AbortTransaction();

        /// <summary>
        /// Aborts and rolls back a tranaction
        /// </summary>
        async public void AbortAsync() => await Session.AbortTransactionAsync();

        public Update<T> Update<T>() where T : Entity
        {
            return new Update<T>(Session);
        }

        public Find<T> Find<T>() where T : Entity
        {
            return new Find<T>(Session);
        }

        public Find<T, TProjection> Find<T, TProjection>() where T : Entity
        {
            return new Find<T, TProjection>(Session);
        }

        public void Save<T>(T entity) where T : Entity
        {
            DB.Save<T>(entity, Session);
        }

        async public Task SaveAsync<T>(T entity) where T : Entity
        {
            await DB.SaveAsync<T>(entity, Session);
        }

        public void Delete<T>(string ID) where T : Entity
        {
            DB.Delete<T>(ID, Session);
        }

        async public Task DeleteAsync<T>(string ID) where T : Entity
        {
            await DB.DeleteAsync<T>(ID, Session);
        }

        public void Delete<T>(Expression<Func<T, bool>> expression) where T : Entity
        {
            DB.Delete<T>(expression, Session);
        }

        async public Task DeleteAsync<T>(Expression<Func<T, bool>> expression) where T : Entity
        {
            await DB.DeleteAsync<T>(expression, Session);
        }

        public void Delete<T>(IEnumerable<String> IDs) where T : Entity
        {
            DB.Delete<T>(IDs, Session);
        }

        async public Task DeleteAsync<T>(IEnumerable<String> IDs) where T : Entity
        {
            await DB.DeleteAsync<T>(IDs, Session);
        }

        public List<T> SearchText<T>(string searchTerm, bool caseSensitive = false, FindOptions<T, T> options = null)
        {
            return DB.SearchText<T>(searchTerm, caseSensitive, options, Session);
        }

        async public Task<List<T>> SearchTextAsync<T>(string searchTerm, bool caseSensitive = false, FindOptions<T, T> options = null)
        {
            return await DB.SearchTextAsync<T>(searchTerm, caseSensitive, options, Session);
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
