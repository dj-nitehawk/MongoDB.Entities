using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    //Make these interface implmentation explicit, so we can fine-tune the api return result
    public partial class MongoServerContext
    {
        public ICluster Cluster => Client.Cluster;

        public MongoClientSettings Settings => Client.Settings;

        void IMongoClient.DropDatabase(string name, CancellationToken cancellationToken)
        {
            Client.DropDatabase(name, cancellationToken);
        }

        void IMongoClient.DropDatabase(IClientSessionHandle session, string name, CancellationToken cancellationToken)
        {
            Client.DropDatabase(session, name, cancellationToken);
        }

        Task IMongoClient.DropDatabaseAsync(string name, CancellationToken cancellationToken)
        {
            return Client.DropDatabaseAsync(name, cancellationToken);
        }

        Task IMongoClient.DropDatabaseAsync(IClientSessionHandle session, string name, CancellationToken cancellationToken)
        {
            return Client.DropDatabaseAsync(session, name, cancellationToken);
        }

        public IMongoDatabase GetDatabase(string name, MongoDatabaseSettings? settings = null)
        {
            return Client.GetDatabase(name, settings);
        }

        IAsyncCursor<string> IMongoClient.ListDatabaseNames(CancellationToken cancellationToken)
        {
            return Client.ListDatabaseNames(cancellationToken);
        }

        IAsyncCursor<string> IMongoClient.ListDatabaseNames(ListDatabaseNamesOptions options, CancellationToken cancellationToken)
        {
            return Client.ListDatabaseNames(options, cancellationToken);
        }

        IAsyncCursor<string> IMongoClient.ListDatabaseNames(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            return Client.ListDatabaseNames(session, cancellationToken);
        }

        IAsyncCursor<string> IMongoClient.ListDatabaseNames(IClientSessionHandle session, ListDatabaseNamesOptions options, CancellationToken cancellationToken)
        {
            return Client.ListDatabaseNames(session, options, cancellationToken);
        }

        Task<IAsyncCursor<string>> IMongoClient.ListDatabaseNamesAsync(CancellationToken cancellationToken)
        {
            return Client.ListDatabaseNamesAsync(cancellationToken);
        }

        Task<IAsyncCursor<string>> IMongoClient.ListDatabaseNamesAsync(ListDatabaseNamesOptions options, CancellationToken cancellationToken)
        {
            return Client.ListDatabaseNamesAsync(options, cancellationToken);
        }

        Task<IAsyncCursor<string>> IMongoClient.ListDatabaseNamesAsync(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            return Client.ListDatabaseNamesAsync(session, cancellationToken);
        }

        Task<IAsyncCursor<string>> IMongoClient.ListDatabaseNamesAsync(IClientSessionHandle session, ListDatabaseNamesOptions options, CancellationToken cancellationToken)
        {
            return Client.ListDatabaseNamesAsync(session, options, cancellationToken);
        }

        IAsyncCursor<BsonDocument> IMongoClient.ListDatabases(CancellationToken cancellationToken)
        {
            return Client.ListDatabases(cancellationToken);
        }

        IAsyncCursor<BsonDocument> IMongoClient.ListDatabases(ListDatabasesOptions options, CancellationToken cancellationToken)
        {
            return Client.ListDatabases(options, cancellationToken);
        }

        IAsyncCursor<BsonDocument> IMongoClient.ListDatabases(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            return Client.ListDatabases(session, cancellationToken);
        }

        IAsyncCursor<BsonDocument> IMongoClient.ListDatabases(IClientSessionHandle session, ListDatabasesOptions options, CancellationToken cancellationToken)
        {
            return Client.ListDatabases(session, options, cancellationToken);
        }

        Task<IAsyncCursor<BsonDocument>> IMongoClient.ListDatabasesAsync(CancellationToken cancellationToken)
        {
            return Client.ListDatabasesAsync(cancellationToken);
        }

        Task<IAsyncCursor<BsonDocument>> IMongoClient.ListDatabasesAsync(ListDatabasesOptions options, CancellationToken cancellationToken)
        {
            return Client.ListDatabasesAsync(options, cancellationToken);
        }

        Task<IAsyncCursor<BsonDocument>> IMongoClient.ListDatabasesAsync(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            return Client.ListDatabasesAsync(session, cancellationToken);
        }

        Task<IAsyncCursor<BsonDocument>> IMongoClient.ListDatabasesAsync(IClientSessionHandle session, ListDatabasesOptions options, CancellationToken cancellationToken)
        {
            return Client.ListDatabasesAsync(session, options, cancellationToken);
        }

        IClientSessionHandle IMongoClient.StartSession(ClientSessionOptions options, CancellationToken cancellationToken)
        {
            return Client.StartSession(options, cancellationToken);
        }

        Task<IClientSessionHandle> IMongoClient.StartSessionAsync(ClientSessionOptions options, CancellationToken cancellationToken)
        {
            return Client.StartSessionAsync(options, cancellationToken);
        }

        IChangeStreamCursor<TResult> IMongoClient.Watch<TResult>(PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions options, CancellationToken cancellationToken)
        {
            return Client.Watch(pipeline, options, cancellationToken);
        }

        IChangeStreamCursor<TResult> IMongoClient.Watch<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions options, CancellationToken cancellationToken)
        {
            return Client.Watch(session, pipeline, options, cancellationToken);
        }

        Task<IChangeStreamCursor<TResult>> IMongoClient.WatchAsync<TResult>(PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions options, CancellationToken cancellationToken)
        {
            return Client.WatchAsync(pipeline, options, cancellationToken);
        }

        Task<IChangeStreamCursor<TResult>> IMongoClient.WatchAsync<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline, ChangeStreamOptions options, CancellationToken cancellationToken = default)
        {
            return Client.WatchAsync(session, pipeline, options, cancellationToken);
        }

        IMongoClient IMongoClient.WithReadConcern(ReadConcern readConcern)
        {
            return Client.WithReadConcern(readConcern);
        }

        IMongoClient IMongoClient.WithReadPreference(ReadPreference readPreference)
        {
            return Client.WithReadPreference(readPreference);
        }

        IMongoClient IMongoClient.WithWriteConcern(WriteConcern writeConcern)
        {
            return Client.WithWriteConcern(writeConcern);
        }
    }
}
