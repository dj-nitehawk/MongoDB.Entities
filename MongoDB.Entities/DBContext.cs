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
    /// This db context class can be used as an alternative entry point instead of the DB static class. 
    /// All methods on this class can be overriden if needed.
    /// </summary>
    public class DBContext
    {
        public IClientSessionHandle Session { get; internal set; }

        /// <summary>
        /// Instantiates a db context
        /// </summary>
        /// <param name="session">An optional session if using within a transaction</param>
        public DBContext(IClientSessionHandle session = null)
            => Session = session;

        /// <summary>
        /// Gets an accurate count of how many entities are matched for a given expression/filter in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="expression">A lambda expression for getting the count for a subset of the data</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<long> CountAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.CountAsync(expression, Session, cancellation);
        }

        /// <summary>
        /// Gets an accurate count of how many total entities are in the collection for a given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<long> CountAsync<T>(CancellationToken cancellation = default) where T : IEntity
        {
            return DB.CountAsync<T>(_ => true, Session, cancellation);
        }

        /// <summary>
        /// Gets an accurate count of how many total entities are in the collection for a given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="filter">A filter definition</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<long> CountAsync<T>(FilterDefinition<T> filter, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.CountAsync(filter, Session, cancellation);
        }

        /// <summary>
        /// Gets an accurate count of how many total entities are in the collection for a given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<long> CountAsync<T>(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.CountAsync(filter, Session, cancellation);
        }

        /// <summary>
        /// Starts an update command for the given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        public virtual Update<T> Update<T>() where T : IEntity
        {
            return new Update<T>(Session);
        }

        /// <summary>
        /// Starts an update-and-get command for the given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        public virtual UpdateAndGet<T> UpdateAndGet<T>() where T : IEntity
        {
            return new UpdateAndGet<T>(Session);
        }

        /// <summary>
        /// Starts an update-and-get command with projection support for the given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TProjection">The type of the end result</typeparam>
        public virtual UpdateAndGet<T, TProjection> UpdateAndGet<T, TProjection>() where T : IEntity
        {
            return new UpdateAndGet<T, TProjection>(Session);
        }

        /// <summary>
        /// Starts a find command for the given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        public virtual Find<T> Find<T>() where T : IEntity
        {
            return new Find<T>(Session);
        }

        /// <summary>
        /// Starts a find command with projection support for the given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TProjection">The type of the end result</typeparam>
        public virtual Find<T, TProjection> Find<T, TProjection>() where T : IEntity
        {
            return new Find<T, TProjection>(Session);
        }

        /// <summary>
        /// Exposes the MongoDB collection for the given entity type as IAggregateFluent in order to facilitate Fluent queries in the transaction sope.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        public virtual IAggregateFluent<T> Fluent<T>(AggregateOptions options = null) where T : IEntity
        {
            return DB.Fluent<T>(options, Session);
        }

        /// <summary>
        /// Exposes the MongoDB collection for the given entity type as IQueryable in order to facilitate LINQ queries in the transaction scope.
        /// </summary>
        /// <param name="options">The aggregate options</param>
        /// <typeparam name="T">The type of entity</typeparam>
        public virtual IMongoQueryable<T> Queryable<T>(AggregateOptions options = null) where T : IEntity
        {
            return DB.Queryable<T>(options, Session);
        }

        /// <summary>
        /// Executes an aggregation pipeline in the transaction scope by supplying a 'Template' object.
        /// Gets a cursor back as the result.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<IAsyncCursor<TResult>> PipelineCursorAsync<T, TResult>(Template<T, TResult> template, AggregateOptions options = null, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.PipelineCursorAsync(template, options, Session, cancellation);
        }

        /// <summary>
        /// Executes an aggregation pipeline in the transaction scope by supplying a 'Template' object.
        /// Gets a list back as the result.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<List<TResult>> PipelineAsync<T, TResult>(Template<T, TResult> template, AggregateOptions options = null, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.PipelineAsync(template, options, Session, cancellation);
        }

        /// <summary>
        /// Executes an aggregation pipeline in the transaction scope by supplying a 'Template' object.
        /// Gets a single or default value as the result.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<TResult> PipelineSingleAsync<T, TResult>(Template<T, TResult> template, AggregateOptions options = null, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.PipelineSingleAsync(template, options, Session, cancellation);
        }

        /// <summary>
        /// Executes an aggregation pipeline in the transaction scope by supplying a 'Template' object.
        /// Gets the first or default value as the result.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<TResult> PipelineFirstAsync<T, TResult>(Template<T, TResult> template, AggregateOptions options = null, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.PipelineFirstAsync(template, options, Session, cancellation);
        }

        /// <summary>
        /// Start a fluent aggregation pipeline with a $GeoNear stage with the supplied parameters in the transaction scope.
        /// </summary>
        /// <param name="NearCoordinates">The coordinates from which to find documents from</param>
        /// <param name="DistanceField">x => x.Distance</param>
        /// <param name="Spherical">Calculate distances using spherical geometry or not</param>
        /// <param name="MaxDistance">The maximum distance in meters from the center point that the documents can be</param>
        /// <param name="MinDistance">The minimum distance in meters from the center point that the documents can be</param>
        /// <param name="Limit">The maximum number of documents to return</param>
        /// <param name="Query">Limits the results to the documents that match the query</param>
        /// <param name="DistanceMultiplier">The factor to multiply all distances returned by the query</param>
        /// <param name="IncludeLocations">Specify the output field to store the point used to calculate the distance</param>
        /// <param name="IndexKey"></param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <typeparam name="T">The type of entity</typeparam>
        public virtual IAggregateFluent<T> GeoNear<T>(Coordinates2D NearCoordinates, Expression<Func<T, object>> DistanceField, bool Spherical = true, int? MaxDistance = null, int? MinDistance = null, int? Limit = null, BsonDocument Query = null, int? DistanceMultiplier = null, Expression<Func<T, object>> IncludeLocations = null, string IndexKey = null, AggregateOptions options = null) where T : IEntity
        {
            return DB.FluentGeoNear(NearCoordinates, DistanceField, Spherical, MaxDistance, MinDistance, Limit, Query, DistanceMultiplier, IncludeLocations, IndexKey, options, Session);
        }

        /// <summary>
        /// Persists an entity to MongoDB in the transaction scope
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="entity">The instance to persist</param>
        /// <param name="cancellation">And optional cancellation token</param>
        public virtual Task<ReplaceOneResult> SaveAsync<T>(T entity, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.SaveAsync(entity, Session, cancellation);
        }

        /// <summary>
        /// Saves an entity partially by specifying a subset of properties in the transaction scope. 
        /// The properties to be saved can be specified with a 'New' expression. 
        /// <para>TIP: The 'New' expression should specify only root level properties.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<UpdateResult> SaveAsync<T>(T entity, Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.SaveAsync(entity, members, Session, cancellation);
        }

        /// <summary>
        /// Persists multiple entities to MongoDB in a single bulk operation in the transaction scope
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="entities">The entities to persist</param>
        /// <param name="cancellation">And optional cancellation token</param>
        public virtual Task<BulkWriteResult<T>> SaveAsync<T>(IEnumerable<T> entities, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.SaveAsync(entities, Session, cancellation);
        }

        /// <summary>
        /// Saves a batch of entities partially by specifying a subset of properties in the transaction scope. 
        /// The properties to be saved can be specified with a 'New' expression. 
        /// <para>TIP: The 'New' expression should specify only root level properties.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The batch of entities to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<BulkWriteResult<T>> SaveAsync<T>(IEnumerable<T> entities, Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.SaveAsync(entities, members, Session, cancellation);
        }

        /// <summary>
        /// Saves an entity while preserving some property values in the database in the transaction scope.
        /// The properties to be preserved can be specified with a 'New' expression or using the [Preserve] or [DontPreserve] attributes.
        /// <para>TIP: The 'New' expression should specify only root level properties.</para>
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="preservation">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<UpdateResult> SavePreservingAsync<T>(T entity, Expression<Func<T, object>> preservation = null, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.SavePreservingAsync(entity, preservation, Session, cancellation);
        }

        /// <summary>
        /// Deletes a single entity from MongoDB in the transaction scope.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="ID">The Id of the entity to delete</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<DeleteResult> DeleteAsync<T>(string ID, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.DeleteAsync<T>(ID, Session, cancellation);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB in the transaction scope
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<DeleteResult> DeleteAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.DeleteAsync(expression, Session, cancellation);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB in the transaction scope
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="IDs">An IEnumerable of entity IDs</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<DeleteResult> DeleteAsync<T>(IEnumerable<string> IDs, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.DeleteAsync<T>(IDs, Session, cancellation);
        }

        /// <summary>
        /// Start a fluent aggregation pipeline with a $text stage with the supplied parameters in the transaction scope.
        /// <para>TIP: Make sure to define a text index with DB.Index&lt;T&gt;() before searching</para>
        /// </summary>
        /// <param name="searchType">The type of text matching to do</param>
        /// <param name="searchTerm">The search term</param>
        /// <param name="caseSensitive">Case sensitivity of the search (optional)</param>
        /// <param name="diacriticSensitive">Diacritic sensitivity of the search (optional)</param>
        /// <param name="language">The language for the search (optional)</param>
        /// <param name="options">Options for finding documents (not required)</param>
        public virtual IAggregateFluent<T> FluentTextSearch<T>(Search searchType, string searchTerm, bool caseSensitive = false, bool diacriticSensitive = false, string language = null, AggregateOptions options = null) where T : IEntity
        {
            return DB.FluentTextSearch<T>(searchType, searchTerm, caseSensitive, diacriticSensitive, language, options, Session);
        }
    }
}
