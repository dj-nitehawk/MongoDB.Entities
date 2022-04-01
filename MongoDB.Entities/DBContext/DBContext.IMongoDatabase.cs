using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
#nullable enable
namespace MongoDB.Entities
{
    public partial class DBContext
    {
        IAsyncCursor<TResult> IMongoDatabase.Aggregate<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken)
        {
            return Database.Aggregate(pipeline, options, cancellationToken);
        }

        IAsyncCursor<TResult> IMongoDatabase.Aggregate<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken)
        {
            return Database.Aggregate(session, pipeline, options, cancellationToken);
        }

        Task<IAsyncCursor<TResult>> IMongoDatabase.AggregateAsync<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken)
        {
            return Database.AggregateAsync(pipeline, options, cancellationToken);
        }

        Task<IAsyncCursor<TResult>> IMongoDatabase.AggregateAsync<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions options, CancellationToken cancellationToken)
        {
            return Database.AggregateAsync(session, pipeline, options, cancellationToken);
        }

        public void AggregateToCollection<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            Database.AggregateToCollection(pipeline, options, cancellationToken);
        }

        public void AggregateToCollection<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            Database.AggregateToCollection(session, pipeline, options, cancellationToken);
        }

        public Task AggregateToCollectionAsync<TResult>(PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Database.AggregateToCollectionAsync(pipeline, options, cancellationToken);
        }

        public Task AggregateToCollectionAsync<TResult>(IClientSessionHandle session, PipelineDefinition<NoPipelineInput, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
        {
            return Database.AggregateToCollectionAsync(session, pipeline, options, cancellationToken);
        }

        public void CreateCollection(string name, CreateCollectionOptions? options = null, CancellationToken cancellationToken = default)
        {
            Database.CreateCollection(name, options, cancellationToken);
        }

        public void CreateCollection(IClientSessionHandle session, string name, CreateCollectionOptions options, CancellationToken cancellationToken)
        {
            Database.CreateCollection(session, name, options, cancellationToken);
        }

        public Task CreateCollectionAsync(string name, CreateCollectionOptions options, CancellationToken cancellationToken)
        {
            return Database.CreateCollectionAsync(name, options, cancellationToken);
        }

        public Task CreateCollectionAsync(IClientSessionHandle session, string name, CreateCollectionOptions options, CancellationToken cancellationToken)
        {
            return Database.CreateCollectionAsync(session, name, options, cancellationToken);
        }

        public void CreateView<TDocument, TResult>(string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument> options, CancellationToken cancellationToken)
        {
            Database.CreateView(viewName, viewOn, pipeline, options, cancellationToken);
        }

        public void CreateView<TDocument, TResult>(IClientSessionHandle session, string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument> options, CancellationToken cancellationToken)
        {
            Database.CreateView(session, viewName, viewOn, pipeline, options, cancellationToken);
        }

        public Task CreateViewAsync<TDocument, TResult>(string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument> options, CancellationToken cancellationToken)
        {
            return Database.CreateViewAsync(viewName, viewOn, pipeline, options, cancellationToken);
        }

        public Task CreateViewAsync<TDocument, TResult>(IClientSessionHandle session, string viewName, string viewOn, PipelineDefinition<TDocument, TResult> pipeline, CreateViewOptions<TDocument> options, CancellationToken cancellationToken)
        {
            return Database.CreateViewAsync(session, viewName, viewOn, pipeline, options, cancellationToken);
        }

        public void DropCollection(string name, CancellationToken cancellationToken = default)
        {
            Database.DropCollection(name, cancellationToken);
        }

        public void DropCollection(IClientSessionHandle session, string name, CancellationToken cancellationToken = default)
        {
            Database.DropCollection(session, name, cancellationToken);
        }

        public Task DropCollectionAsync(string name, CancellationToken cancellationToken = default)
        {
            return Database.DropCollectionAsync(name, cancellationToken);
        }

        public Task DropCollectionAsync(IClientSessionHandle session, string name, CancellationToken cancellationToken = default)
        {
            return Database.DropCollectionAsync(session, name, cancellationToken);
        }

        public DBCollection<TDocument> GetCollection<TDocument>(string name, MongoCollectionSettings? settings = null)
        {
            return new(Database.GetCollection<TDocument>(name, settings));
        }
        IMongoCollection<TDocument> IMongoDatabase.GetCollection<TDocument>(string name, MongoCollectionSettings? settings)
        {
            return Database.GetCollection<TDocument>(name, settings);
        }

        IAsyncCursor<string> IMongoDatabase.ListCollectionNames(ListCollectionNamesOptions options, CancellationToken cancellationToken)
        {
            return Database.ListCollectionNames(options, cancellationToken);
        }

        IAsyncCursor<string> IMongoDatabase.ListCollectionNames(IClientSessionHandle session, ListCollectionNamesOptions options, CancellationToken cancellationToken)
        {
            return Database.ListCollectionNames(session, options, cancellationToken);
        }

        public Task<IAsyncCursor<string>> ListCollectionNamesAsync(ListCollectionNamesOptions options, CancellationToken cancellationToken)
        {
            return Database.ListCollectionNamesAsync(options, cancellationToken);
        }

        public Task<IAsyncCursor<string>> ListCollectionNamesAsync(IClientSessionHandle session, ListCollectionNamesOptions options, CancellationToken cancellationToken)
        {
            return Database.ListCollectionNamesAsync(session, options, cancellationToken);
        }

        IAsyncCursor<Bson.BsonDocument> IMongoDatabase.ListCollections(ListCollectionsOptions options, CancellationToken cancellationToken)
        {
            return Database.ListCollections(options, cancellationToken);
        }

        IAsyncCursor<Bson.BsonDocument> IMongoDatabase.ListCollections(IClientSessionHandle session, ListCollectionsOptions options, CancellationToken cancellationToken)
        {
            return Database.ListCollections(session, options, cancellationToken);
        }

        Task<IAsyncCursor<Bson.BsonDocument>> IMongoDatabase.ListCollectionsAsync(ListCollectionsOptions options, CancellationToken cancellationToken)
        {
            return Database.ListCollectionsAsync(options, cancellationToken);
        }

        Task<IAsyncCursor<Bson.BsonDocument>> IMongoDatabase.ListCollectionsAsync(IClientSessionHandle session, ListCollectionsOptions options, CancellationToken cancellationToken)
        {
            return Database.ListCollectionsAsync(session, options, cancellationToken);
        }

        void IMongoDatabase.RenameCollection(string oldName, string newName, RenameCollectionOptions options, CancellationToken cancellationToken)
        {
            Database.RenameCollection(oldName, newName, options, cancellationToken);
        }

        void IMongoDatabase.RenameCollection(IClientSessionHandle session, string oldName, string newName, RenameCollectionOptions options, CancellationToken cancellationToken)
        {
            Database.RenameCollection(session, oldName, newName, options, cancellationToken);
        }

        Task IMongoDatabase.RenameCollectionAsync(string oldName, string newName, RenameCollectionOptions options, CancellationToken cancellationToken)
        {
            return Database.RenameCollectionAsync(oldName, newName, options, cancellationToken);
        }

        Task IMongoDatabase.RenameCollectionAsync(IClientSessionHandle session, string oldName, string newName, RenameCollectionOptions options, CancellationToken cancellationToken)
        {
            return Database.RenameCollectionAsync(session, oldName, newName, options, cancellationToken);
        }

        TResult IMongoDatabase.RunCommand<TResult>(Command<TResult> command, ReadPreference readPreference, CancellationToken cancellationToken)
        {
            return Database.RunCommand(command, readPreference, cancellationToken);
        }

        TResult IMongoDatabase.RunCommand<TResult>(IClientSessionHandle session, Command<TResult> command, ReadPreference readPreference, CancellationToken cancellationToken)
        {
            return Database.RunCommand(session, command, readPreference, cancellationToken);
        }

        Task<TResult> IMongoDatabase.RunCommandAsync<TResult>(Command<TResult> command, ReadPreference readPreference, CancellationToken cancellationToken)
        {
            return Database.RunCommandAsync(command, readPreference, cancellationToken);
        }

        Task<TResult> IMongoDatabase.RunCommandAsync<TResult>(IClientSessionHandle session, Command<TResult> command, ReadPreference readPreference, CancellationToken cancellationToken)
        {
            return Database.RunCommandAsync(session, command, readPreference, cancellationToken);
        }

        IChangeStreamCursor<TResult> IMongoDatabase.Watch<TResult>(PipelineDefinition<ChangeStreamDocument<Bson.BsonDocument>, TResult> pipeline, ChangeStreamOptions options, CancellationToken cancellationToken)
        {
            return Database.Watch(pipeline, options, cancellationToken);
        }

        IChangeStreamCursor<TResult> IMongoDatabase.Watch<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<Bson.BsonDocument>, TResult> pipeline, ChangeStreamOptions options, CancellationToken cancellationToken)
        {
            return Database.Watch(session, pipeline, options, cancellationToken);
        }

        Task<IChangeStreamCursor<TResult>> IMongoDatabase.WatchAsync<TResult>(PipelineDefinition<ChangeStreamDocument<Bson.BsonDocument>, TResult> pipeline, ChangeStreamOptions options, CancellationToken cancellationToken)
        {
            return Database.WatchAsync(pipeline, options, cancellationToken);
        }

        Task<IChangeStreamCursor<TResult>> IMongoDatabase.WatchAsync<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<Bson.BsonDocument>, TResult> pipeline, ChangeStreamOptions options, CancellationToken cancellationToken)
        {
            return Database.WatchAsync(session, pipeline, options, cancellationToken);
        }

        IMongoDatabase IMongoDatabase.WithReadConcern(ReadConcern readConcern)
        {
            return Database.WithReadConcern(readConcern);
        }

        IMongoDatabase IMongoDatabase.WithReadPreference(ReadPreference readPreference)
        {
            return Database.WithReadPreference(readPreference);
        }

        IMongoDatabase IMongoDatabase.WithWriteConcern(WriteConcern writeConcern)
        {
            return Database.WithWriteConcern(writeConcern);
        }
    }
}
