using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

//todo: xml docs
//todo: tests

namespace MongoDB.Entities
{
    public class Transaction : IDisposable
    {
        public IClientSessionHandle Session { get; }
        private IMongoClient client;

        public Transaction(ClientSessionOptions options = null)
        {
            client = DB.GetClient();
            Session = client.StartSession(options);
            Session.StartTransaction();
        }

        public void CommitTransaction() => Session.CommitTransaction();
        async public void CommitTransactionAsync() => await Session.CommitTransactionAsync();

        public Index<T> Index<T>() where T : Entity
        {
            return new Index<T>(Session);
        }

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

        public void Dispose()
        {
            Session.Dispose();
        }
    }
}
