using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities;

public partial class DBCollection<T>
{
    public CollectionNamespace CollectionNamespace => Collection.CollectionNamespace;

    public IMongoDatabase Database => Collection.Database;

    public IBsonSerializer<T> DocumentSerializer => Collection.DocumentSerializer;

    public IMongoIndexManager<T> Indexes => Collection.Indexes;

    public MongoCollectionSettings Settings => Collection.Settings;

    public IAsyncCursor<TResult> Aggregate<TResult>(PipelineDefinition<T, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.Aggregate(pipeline, options, cancellationToken);
    }

    public IAsyncCursor<TResult> Aggregate<TResult>(IClientSessionHandle session, PipelineDefinition<T, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.Aggregate(session, pipeline, options, cancellationToken);
    }

    public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<T, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.AggregateAsync(pipeline, options, cancellationToken);
    }

    public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(IClientSessionHandle session, PipelineDefinition<T, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.AggregateAsync(session, pipeline, options, cancellationToken);
    }

    public void AggregateToCollection<TResult>(PipelineDefinition<T, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
    {
        Collection.AggregateToCollection(pipeline, options, cancellationToken);
    }

    public void AggregateToCollection<TResult>(IClientSessionHandle session, PipelineDefinition<T, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
    {
        Collection.AggregateToCollection(session, pipeline, options, cancellationToken);
    }

    public Task AggregateToCollectionAsync<TResult>(PipelineDefinition<T, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.AggregateToCollectionAsync(pipeline, options, cancellationToken);
    }

    public Task AggregateToCollectionAsync<TResult>(IClientSessionHandle session, PipelineDefinition<T, TResult> pipeline, AggregateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.AggregateToCollectionAsync(session, pipeline, options, cancellationToken);
    }

    public BulkWriteResult<T> BulkWrite(IEnumerable<WriteModel<T>> requests, BulkWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.BulkWrite(requests, options, cancellationToken);
    }

    public BulkWriteResult<T> BulkWrite(IClientSessionHandle session, IEnumerable<WriteModel<T>> requests, BulkWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.BulkWrite(session, requests, options, cancellationToken);
    }

    public Task<BulkWriteResult<T>> BulkWriteAsync(IEnumerable<WriteModel<T>> requests, BulkWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.BulkWriteAsync(requests, options, cancellationToken);
    }

    public Task<BulkWriteResult<T>> BulkWriteAsync(IClientSessionHandle session, IEnumerable<WriteModel<T>> requests, BulkWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.BulkWriteAsync(session, requests, options, cancellationToken);
    }

    [Obsolete("Use CountDocuments or EstimatedDocumentCount instead.")]
    long IMongoCollection<T>.Count(FilterDefinition<T> filter, CountOptions options, CancellationToken cancellationToken)
    {
        return Collection.Count(filter, options, cancellationToken);
    }

    [Obsolete("Use CountDocuments or EstimatedDocumentCount instead.")]
    long IMongoCollection<T>.Count(IClientSessionHandle session, FilterDefinition<T> filter, CountOptions options, CancellationToken cancellationToken)
    {
        return Collection.Count(session, filter, options, cancellationToken);
    }

    [Obsolete("Use CountDocumentsAsync or EstimatedDocumentCountAsync instead.")]
    Task<long> IMongoCollection<T>.CountAsync(FilterDefinition<T> filter, CountOptions? options, CancellationToken cancellationToken)
    {
        return Collection.CountAsync(filter, options, cancellationToken);
    }

    [Obsolete("Use CountDocumentsAsync or EstimatedDocumentCountAsync instead.")]
    Task<long> IMongoCollection<T>.CountAsync(IClientSessionHandle session, FilterDefinition<T> filter, CountOptions? options, CancellationToken cancellationToken)
    {
        return Collection.CountAsync(session, filter, options, cancellationToken);
    }

    public long CountDocuments(FilterDefinition<T> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.CountDocuments(filter, options, cancellationToken);
    }

    public long CountDocuments(IClientSessionHandle session, FilterDefinition<T> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.CountDocuments(session, filter, options, cancellationToken);
    }

    public Task<long> CountDocumentsAsync(FilterDefinition<T> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.CountDocumentsAsync(filter, options, cancellationToken);
    }

    public Task<long> CountDocumentsAsync(IClientSessionHandle session, FilterDefinition<T> filter, CountOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.CountDocumentsAsync(session, filter, options, cancellationToken);
    }

    public DeleteResult DeleteMany(FilterDefinition<T> filter, CancellationToken cancellationToken = default)
    {
        return Collection.DeleteMany(filter, cancellationToken);
    }

    public DeleteResult DeleteMany(FilterDefinition<T> filter, DeleteOptions options, CancellationToken cancellationToken = default)
    {
        return Collection.DeleteMany(filter, options, cancellationToken);
    }

    public DeleteResult DeleteMany(IClientSessionHandle session, FilterDefinition<T> filter, DeleteOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.DeleteMany(session, filter, options, cancellationToken);
    }

    public Task<DeleteResult> DeleteManyAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default)
    {
        return Collection.DeleteManyAsync(filter, cancellationToken);
    }

    public Task<DeleteResult> DeleteManyAsync(FilterDefinition<T> filter, DeleteOptions options, CancellationToken cancellationToken = default)
    {
        return Collection.DeleteManyAsync(filter, options, cancellationToken);
    }

    public Task<DeleteResult> DeleteManyAsync(IClientSessionHandle session, FilterDefinition<T> filter, DeleteOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.DeleteManyAsync(session, filter, options, cancellationToken);
    }

    public DeleteResult DeleteOne(FilterDefinition<T> filter, CancellationToken cancellationToken = default)
    {
        return Collection.DeleteOne(filter, cancellationToken);
    }

    public DeleteResult DeleteOne(FilterDefinition<T> filter, DeleteOptions options, CancellationToken cancellationToken = default)
    {
        return Collection.DeleteOne(filter, options, cancellationToken);
    }

    public DeleteResult DeleteOne(IClientSessionHandle session, FilterDefinition<T> filter, DeleteOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.DeleteOne(session, filter, options, cancellationToken);
    }

    public Task<DeleteResult> DeleteOneAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default)
    {
        return Collection.DeleteOneAsync(filter, cancellationToken);
    }

    public Task<DeleteResult> DeleteOneAsync(FilterDefinition<T> filter, DeleteOptions options, CancellationToken cancellationToken = default)
    {
        return Collection.DeleteOneAsync(filter, options, cancellationToken);
    }

    public Task<DeleteResult> DeleteOneAsync(IClientSessionHandle session, FilterDefinition<T> filter, DeleteOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.DeleteOneAsync(session, filter, options, cancellationToken);
    }

    public IAsyncCursor<TField> Distinct<TField>(FieldDefinition<T, TField> field, FilterDefinition<T> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.Distinct(field, filter, options, cancellationToken);
    }

    public IAsyncCursor<TField> Distinct<TField>(IClientSessionHandle session, FieldDefinition<T, TField> field, FilterDefinition<T> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.Distinct(session, field, filter, options, cancellationToken);
    }

    public Task<IAsyncCursor<TField>> DistinctAsync<TField>(FieldDefinition<T, TField> field, FilterDefinition<T> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.DistinctAsync(field, filter, options, cancellationToken);
    }

    public Task<IAsyncCursor<TField>> DistinctAsync<TField>(IClientSessionHandle session, FieldDefinition<T, TField> field, FilterDefinition<T> filter, DistinctOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.DistinctAsync(session, field, filter, options, cancellationToken);
    }

    public long EstimatedDocumentCount(EstimatedDocumentCountOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.EstimatedDocumentCount(options, cancellationToken);
    }

    public Task<long> EstimatedDocumentCountAsync(EstimatedDocumentCountOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.EstimatedDocumentCountAsync(options, cancellationToken);
    }

    public Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(FilterDefinition<T> filter, FindOptions<T, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.FindAsync(filter, options, cancellationToken);
    }

    public Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, FindOptions<T, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.FindAsync(session, filter, options, cancellationToken);
    }

    public TProjection FindOneAndDelete<TProjection>(FilterDefinition<T> filter, FindOneAndDeleteOptions<T, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.FindOneAndDelete(filter, options, cancellationToken);
    }

    public TProjection FindOneAndDelete<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, FindOneAndDeleteOptions<T, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.FindOneAndDelete(session, filter, options, cancellationToken);
    }

    public Task<TProjection> FindOneAndDeleteAsync<TProjection>(FilterDefinition<T> filter, FindOneAndDeleteOptions<T, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.FindOneAndDeleteAsync(filter, options, cancellationToken);
    }

    public Task<TProjection> FindOneAndDeleteAsync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, FindOneAndDeleteOptions<T, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.FindOneAndDeleteAsync(session, filter, options, cancellationToken);
    }

    public TProjection FindOneAndReplace<TProjection>(FilterDefinition<T> filter, T replacement, FindOneAndReplaceOptions<T, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.FindOneAndReplace(filter, replacement, options, cancellationToken);
    }

    public TProjection FindOneAndReplace<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, FindOneAndReplaceOptions<T, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.FindOneAndReplace(session, filter, replacement, options, cancellationToken);
    }

    public Task<TProjection> FindOneAndReplaceAsync<TProjection>(FilterDefinition<T> filter, T replacement, FindOneAndReplaceOptions<T, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.FindOneAndReplaceAsync(filter, replacement, options, cancellationToken);
    }

    public Task<TProjection> FindOneAndReplaceAsync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, FindOneAndReplaceOptions<T, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.FindOneAndReplaceAsync(session, filter, replacement, options, cancellationToken);
    }

    public TProjection FindOneAndUpdate<TProjection>(FilterDefinition<T> filter, UpdateDefinition<T> update, FindOneAndUpdateOptions<T, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.FindOneAndUpdate(filter, update, options, cancellationToken);
    }

    public TProjection FindOneAndUpdate<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, FindOneAndUpdateOptions<T, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.FindOneAndUpdate(session, filter, update, options, cancellationToken);
    }

    public Task<TProjection> FindOneAndUpdateAsync<TProjection>(FilterDefinition<T> filter, UpdateDefinition<T> update, FindOneAndUpdateOptions<T, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
    }

    public Task<TProjection> FindOneAndUpdateAsync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, FindOneAndUpdateOptions<T, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.FindOneAndUpdateAsync(session, filter, update, options, cancellationToken);
    }

    public IAsyncCursor<TProjection> FindSync<TProjection>(FilterDefinition<T> filter, FindOptions<T, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.FindSync(filter, options, cancellationToken);
    }

    public IAsyncCursor<TProjection> FindSync<TProjection>(IClientSessionHandle session, FilterDefinition<T> filter, FindOptions<T, TProjection>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.FindSync(session, filter, options, cancellationToken);
    }

    public void InsertMany(IEnumerable<T> documents, InsertManyOptions? options = null, CancellationToken cancellationToken = default)
    {
        Collection.InsertMany(documents, options, cancellationToken);
    }

    public void InsertMany(IClientSessionHandle session, IEnumerable<T> documents, InsertManyOptions? options = null, CancellationToken cancellationToken = default)
    {
        Collection.InsertMany(session, documents, options, cancellationToken);
    }

    public Task InsertManyAsync(IEnumerable<T> documents, InsertManyOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.InsertManyAsync(documents, options, cancellationToken);
    }

    public Task InsertManyAsync(IClientSessionHandle session, IEnumerable<T> documents, InsertManyOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.InsertManyAsync(session, documents, options, cancellationToken);
    }

    public void InsertOne(T document, InsertOneOptions? options = null, CancellationToken cancellationToken = default)
    {
        Collection.InsertOne(document, options, cancellationToken);
    }

    public void InsertOne(IClientSessionHandle session, T document, InsertOneOptions? options = null, CancellationToken cancellationToken = default)
    {
        Collection.InsertOne(session, document, options, cancellationToken);
    }

    [Obsolete("Use the new overload of InsertOneAsync with an InsertOneOptions parameter instead.")]
    Task IMongoCollection<T>.InsertOneAsync(T document, CancellationToken _cancellationToken)
    {
        return Collection.InsertOneAsync(document, _cancellationToken);
    }

    public Task InsertOneAsync(T document, InsertOneOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.InsertOneAsync(document, options, cancellationToken);
    }

    public Task InsertOneAsync(IClientSessionHandle session, T document, InsertOneOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.InsertOneAsync(session, document, options, cancellationToken);
    }

    public IAsyncCursor<TResult> MapReduce<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<T, TResult>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.MapReduce(map, reduce, options, cancellationToken);
    }

    public IAsyncCursor<TResult> MapReduce<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<T, TResult>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.MapReduce(session, map, reduce, options, cancellationToken);
    }

    public Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<T, TResult>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.MapReduceAsync(map, reduce, options, cancellationToken);
    }

    public Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<T, TResult>? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.MapReduceAsync(session, map, reduce, options, cancellationToken);
    }

    public IFilteredMongoCollection<TDerivedDocument> OfType<TDerivedDocument>() where TDerivedDocument : T
    {
        return Collection.OfType<TDerivedDocument>();
    }

    public ReplaceOneResult ReplaceOne(FilterDefinition<T> filter, T replacement, ReplaceOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.ReplaceOne(filter, replacement, options, cancellationToken);
    }

    [Obsolete("Use the overload that takes a ReplaceOptions instead of an UpdateOptions.")]
    ReplaceOneResult IMongoCollection<T>.ReplaceOne(FilterDefinition<T> filter, T replacement, UpdateOptions options, CancellationToken cancellationToken)
    {
        return Collection.ReplaceOne(filter, replacement, options, cancellationToken);
    }

    public ReplaceOneResult ReplaceOne(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, ReplaceOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.ReplaceOne(session, filter, replacement, options, cancellationToken);
    }

    [Obsolete("Use the overload that takes a ReplaceOptions instead of an UpdateOptions.")]
    ReplaceOneResult IMongoCollection<T>.ReplaceOne(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, UpdateOptions options, CancellationToken cancellationToken)
    {
        return Collection.ReplaceOne(session, filter, replacement, options, cancellationToken);
    }

    public Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<T> filter, T replacement, ReplaceOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.ReplaceOneAsync(filter, replacement, options, cancellationToken);
    }

    [Obsolete("Use the overload that takes a ReplaceOptions instead of an UpdateOptions.")]
    Task<ReplaceOneResult> IMongoCollection<T>.ReplaceOneAsync(FilterDefinition<T> filter, T replacement, UpdateOptions options, CancellationToken cancellationToken)
    {
        return Collection.ReplaceOneAsync(filter, replacement, options, cancellationToken);
    }

    public Task<ReplaceOneResult> ReplaceOneAsync(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, ReplaceOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.ReplaceOneAsync(session, filter, replacement, options, cancellationToken);
    }

    [Obsolete("Use the overload that takes a ReplaceOptions instead of an UpdateOptions.")]
    Task<ReplaceOneResult> IMongoCollection<T>.ReplaceOneAsync(IClientSessionHandle session, FilterDefinition<T> filter, T replacement, UpdateOptions options, CancellationToken cancellationToken)
    {
        return Collection.ReplaceOneAsync(session, filter, replacement, options, cancellationToken);
    }

    public UpdateResult UpdateMany(FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.UpdateMany(filter, update, options, cancellationToken);
    }

    public UpdateResult UpdateMany(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.UpdateMany(session, filter, update, options, cancellationToken);
    }

    public Task<UpdateResult> UpdateManyAsync(FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.UpdateManyAsync(filter, update, options, cancellationToken);
    }

    public Task<UpdateResult> UpdateManyAsync(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.UpdateManyAsync(session, filter, update, options, cancellationToken);
    }

    public UpdateResult UpdateOne(FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.UpdateOne(filter, update, options, cancellationToken);
    }

    public UpdateResult UpdateOne(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.UpdateOne(session, filter, update, options, cancellationToken);
    }

    public Task<UpdateResult> UpdateOneAsync(FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.UpdateOneAsync(filter, update, options, cancellationToken);
    }

    public Task<UpdateResult> UpdateOneAsync(IClientSessionHandle session, FilterDefinition<T> filter, UpdateDefinition<T> update, UpdateOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.UpdateOneAsync(session, filter, update, options, cancellationToken);
    }

    public IChangeStreamCursor<TResult> Watch<TResult>(PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.Watch(pipeline, options, cancellationToken);
    }

    public IChangeStreamCursor<TResult> Watch<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.Watch(session, pipeline, options, cancellationToken);
    }

    public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.WatchAsync(pipeline, options, cancellationToken);
    }

    public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<T>, TResult> pipeline, ChangeStreamOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Collection.WatchAsync(session, pipeline, options, cancellationToken);
    }

    public IMongoCollection<T> WithReadConcern(ReadConcern readConcern)
    {
        return Collection.WithReadConcern(readConcern);
    }

    public IMongoCollection<T> WithReadPreference(ReadPreference readPreference)
    {
        return Collection.WithReadPreference(readPreference);
    }

    public IMongoCollection<T> WithWriteConcern(WriteConcern writeConcern)
    {
        return Collection.WithWriteConcern(writeConcern);
    }
}
