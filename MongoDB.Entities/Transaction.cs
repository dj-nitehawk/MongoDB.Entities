using MongoDB.Driver;
using System;

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



        public void Dispose()
        {
            Session.Dispose();
        }
    }
}
