using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public static class Extensions
    {
        private class Holder<T>
        {
            public T Data { get; set; }
        }

        private static T Duplicate<T>(this T source)
        {
            var holder = new Holder<T> { Data = source };
            return BsonSerializer.Deserialize<Holder<T>>(holder.ToBson()).Data;
        }

        internal static void ThrowIfInvalid(this string value)
        {
            if (!ObjectId.TryParse(value, out _)) throw new InvalidOperationException("Please save the entity before performing this operation!");
        }

        internal static void ThrowIfUnsaved(this IEntity entity)
        {
            if (string.IsNullOrEmpty(entity.ID)) throw new InvalidOperationException("Please save the entity before performing this operation!");
        }

        /// <summary>
        /// Gets the name of the database this entity is attached to. Returns name of default database if not attached.
        /// </summary>
        public static string Database(this IEntity entity)
        {
            var attribute = entity.GetType().GetCustomAttribute<DatabaseAttribute>();
            if (attribute != null)
            {
                return attribute.Name;
            }
            return DB.GetInstance(null).DbName;
        }

        /// <summary>
        /// Returns the full dotted path of a property for the given expression
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static string FullPath<T>(this Expression<Func<T, object>> expression)
        {
            return Prop.Path(expression);
        }

        /// <summary>
        /// Registers MongoDB.Entities as a service with the IOC services collection.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="Database">MongoDB database name.</param>
        /// <param name="Host">MongoDB host address. Defaults to 127.0.0.1</param>
        /// <param name="Port">MongoDB port number. Defaults to 27017</param>
        /// <returns></returns>
        public static IServiceCollection AddMongoDBEntities(this IServiceCollection services, string Database, string Host = "127.0.0.1", int Port = 27017)
        {
            services.AddSingleton(new DB(Database, Host, Port));
            return services;
        }

        /// <summary>
        /// Registers MongoDB.Entities as a service with the IOC services collection.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="Settings">A 'MongoClientSettings' object with customized connection parameters such as authentication credentials.</param>
        /// <param name="Database">MongoDB database name.</param>
        /// <returns></returns>
        public static IServiceCollection AddMongoDBEntities(this IServiceCollection services, MongoClientSettings Settings, string Database)
        {
            services.AddSingleton(new DB(Settings, Database));
            return services;
        }

        /// <summary>
        /// Gets the IMongoCollection for a given IEntity type.
        /// <para>TIP: Try never to use this unless really neccessary.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static IMongoCollection<T> Collection<T>(this T entity) where T : IEntity
        {
            return DB.Collection<T>(entity.Database());
        }

        /// <summary>
        /// Gets the collection name for this entity
        /// </summary>
        public static string CollectionName(this IEntity entity)
        {
            var type = entity.GetType();
            var attribute = type.GetCustomAttribute<NameAttribute>();
            if (attribute != null)
            {
                return attribute.Name;
            }
            return type.Name;
        }

        /// <summary>
        /// An IQueryable collection of sibling Entities.
        /// </summary>
        public static IMongoQueryable<T> Queryable<T>(this T entity, AggregateOptions options = null) where T : IEntity
        {
            return DB.Queryable<T>(options, entity.Database());
        }

        /// <summary>
        /// An IAggregateFluent collection of sibling Entities.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public static IAggregateFluent<T> Fluent<T>(this T entity, IClientSessionHandle session = null, AggregateOptions options = null) where T : IEntity
        {
            return DB.Fluent<T>(options, session, entity.Database());
        }

        /// <summary>
        /// Adds a distinct aggregation stage to a fluent pipeline. 
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static IAggregateFluent<T> Distinct<T>(this IAggregateFluent<T> aggregate) where T : IEntity
        {
            PipelineStageDefinition<T, T> groupStage = @"
                                                        {
                                                            $group: {
                                                                _id: '$_id',
                                                                doc: {
                                                                    $first: '$$ROOT'
                                                                }
                                                            }
                                                        }";

            PipelineStageDefinition<T, T> rootStage = @"
                                                        {
                                                            $replaceRoot: {
                                                                newRoot: '$doc'
                                                            }
                                                        }";

            return aggregate.AppendStage(groupStage).AppendStage(rootStage);
        }

        /// <summary>
        /// Appends a match stage to the pipeline with a filter expression
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="aggregate"></param>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        public static IAggregateFluent<T> Match<T>(this IAggregateFluent<T> aggregate, Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter) where T : IEntity
        {
            return aggregate.Match(filter(Builders<T>.Filter));
        }

        /// <summary>
        /// Appends a match stage to the pipeline with an aggregation expression (i.e. $expr)
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="aggregate"></param>
        /// <param name="expression">{ $gt: ['$Property1', '$Property2'] }</param>
        /// <returns></returns>
        public static IAggregateFluent<T> MatchExpression<T>(this IAggregateFluent<T> aggregate, string expression) where T : IEntity
        {
            PipelineStageDefinition<T, T> stage = "{$match:{$expr:" + expression + "}}";

            return aggregate.AppendStage(stage);
        }

        /// <summary>
        /// Returns a reference to this entity.
        /// </summary>
        public static One<T> ToReference<T>(this T entity) where T : IEntity
        {
            return new One<T>(entity);
        }

        /// <summary>
        /// Creates an unlinked duplicate of the original IEntity ready for embedding with a blank ID.
        /// </summary>
        public static T ToDocument<T>(this T entity) where T : IEntity
        {
            var res = entity.Duplicate();
            res.ID = ObjectId.Empty.ToString();
            return res;
        }

        /// <summary>
        /// Creates unlinked duplicates of the original Entities ready for embedding with blank IDs.
        /// </summary>
        public static T[] ToDocuments<T>(this T[] entities) where T : IEntity
        {
            var res = entities.Duplicate();
            foreach (var e in res)
            {
                e.ID = ObjectId.Empty.ToString();
            }
            return res;
        }

        /// <summary>
        ///Creates unlinked duplicates of the original Entities ready for embedding with blank IDs.
        /// </summary>
        public static IEnumerable<T> ToDocuments<T>(this IEnumerable<T> entities) where T : IEntity
        {
            var res = entities.Duplicate();
            foreach (var e in res)
            {
                e.ID = ObjectId.Empty.ToString();
            }
            return res;
        }

        /// <summary>
        /// Replaces an IEntity in the databse if a matching item is found (by ID) or creates a new one if not found.
        /// <para>WARNING: The shape of the IEntity in the database is always owerwritten with the current shape of the IEntity. So be mindful of data loss due to schema changes.</para>
        /// </summary>
        public static ReplaceOneResult Save<T>(this T entity) where T : IEntity
        {
            return Run.Sync(() => SaveAsync(entity));
        }

        /// <summary>
        /// Replaces an IEntity in the databse if a matching item is found (by ID) or creates a new one if not found.
        /// <para>WARNING: The shape of the IEntity in the database is always owerwritten with the current shape of the IEntity. So be mindful of data loss due to schema changes.</para>
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<ReplaceOneResult> SaveAsync<T>(this T entity, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.SaveAsync(entity: entity, db: entity.Database(), cancellation: cancellation);
        }

        /// <summary>
        /// Replaces Entities in the databse if matching items are found (by ID) or creates new ones if not found.
        /// <para>WARNING: The shape of the IEntity in the database is always owerwritten with the current shape of the IEntity. So be mindful of data loss due to schema changes.</para>
        /// </summary>
        public static BulkWriteResult<T> Save<T>(this IEnumerable<T> entities) where T : IEntity
        {
            return Run.Sync(() => SaveAsync(entities));
        }

        /// <summary>
        /// Replaces Entities in the databse if matching items are found (by ID) or creates new ones if not found.
        /// <para>WARNING: The shape of the IEntity in the database is always owerwritten with the current shape of the IEntity. So be mindful of data loss due to schema changes.</para>
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<BulkWriteResult<T>> SaveAsync<T>(this IEnumerable<T> entities, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.SaveAsync(entities: entities, db: entities.First().Database(), cancellation: cancellation);
        }

        /// <summary>
        /// Save this entity while preserving some property values in the database.
        /// The properties to be preserved can be specified with a 'New' expression.
        /// <para>TIP: The 'New' expression should specify only root level properties.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="preservation">x => new { x.PropOne, x.PropTwo }</param>
        public static ReplaceOneResult SavePreserving<T>(this T entity, Expression<Func<T, object>> preservation) where T : IEntity
        {
            return Run.Sync(() => SavePreservingAsync(entity, preservation));
        }

        /// <summary>
        /// Save this entity while preserving some property values in the database.
        /// The properties to be preserved can be specified with a 'New' expression.
        /// <para>TIP: The 'New' expression should specify only root level properties.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="preservation">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<ReplaceOneResult> SavePreservingAsync<T>(this T entity, Expression<Func<T, object>> preservation, CancellationToken cancellation = default) where T : IEntity
        {
            return DB.SavePreservingAsync(entity, preservation, null, entity.Database(), cancellation);
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        public static DeleteResult Delete<T>(this T entity) where T : IEntity
        {
            return Run.Sync(() => DeleteAsync(entity));
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        public static Task<DeleteResult> DeleteAsync<T>(this T entity) where T : IEntity
        {
            return DB.DeleteAsync<T>(ID: entity.ID, db: entity.Database());
        }

        /// <summary>
        /// Deletes multiple entities from the database
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        public static DeleteResult DeleteAll<T>(this IEnumerable<T> entities) where T : IEntity
        {
            return Run.Sync(() => DeleteAllAsync(entities));
        }

        /// <summary>
        /// Deletes multiple entities from the database
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        public static Task<DeleteResult> DeleteAllAsync<T>(this IEnumerable<T> entities) where T : IEntity
        {
            return DB.DeleteAsync<T>(IDs: entities.Select(e => e.ID), db: entities.First().Database());
        }

        /// <summary>
        /// Sort a list of objects by relevance to a given string using Levenshtein Distance
        /// </summary>
        /// <typeparam name="T">Any object type</typeparam>
        /// <param name="objects">The list of objects to sort</param>
        /// <param name="searchTerm">The term to measure relevance to</param>
        /// <param name="propertyToSortBy">x => x.PropertyName [the term will be matched against the value of this property]</param>
        public static IEnumerable<T> SortByRelevance<T>(this IEnumerable<T> objects, string searchTerm, Func<T, string> propertyToSortBy)
        {
            var lev = new Levenshtein(searchTerm);

            return objects.Select(o => new
            {
                score = lev.DistanceFrom(propertyToSortBy(o)),
                obj = o
            })
            .OrderBy(x => x.score)
            .Select(x => x.obj);
        }

        /// <summary>
        /// Converts a search term to Double Metaphone hash code suitable for fuzzy text searching.
        /// </summary>
        /// <param name="term">A single or multiple word search term</param>
        public static string ToDoubleMetaphoneHash(this string term)
        {
            return string.Join(" ", DoubleMetaphone.GetKeys(term));
        }

        /// <summary>
        /// Initializes supplied property with a new One-To-Many relationship.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="propertyToInit">() => PropertyName</param>
        public static void InitOneToMany<TChild>(this IEntity parent, Expression<Func<Many<TChild>>> propertyToInit) where TChild : IEntity
        {
            var body = (MemberExpression)propertyToInit.Body;
            var property = (PropertyInfo)body.Member;
            property.SetValue(parent, new Many<TChild>(parent, property.Name));
        }

        /// <summary>
        /// Initializes supplied property with a new Many-To-Many relationship.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="propertyToInit">() = > PropertyName</param>
        /// <param name="propertyOtherSide">x => x.PropertyName</param>
        public static void InitManyToMany<TChild>(this IEntity parent, Expression<Func<Many<TChild>>> propertyToInit, Expression<Func<TChild, object>> propertyOtherSide) where TChild : IEntity
        {
            var body = (MemberExpression)propertyToInit.Body;
            var property = (PropertyInfo)body.Member;
            var hasOwnerAttrib = property.GetCustomAttributes<OwnerSideAttribute>().Count() > 0;
            var hasInverseAttrib = property.GetCustomAttributes<InverseSideAttribute>().Count() > 0;
            if (hasOwnerAttrib && hasInverseAttrib) throw new InvalidOperationException("Only one type of relationship side attribute is allowed on a property");
            if (!hasOwnerAttrib && !hasInverseAttrib) throw new InvalidOperationException("Missing attribute for determining relationship side of a many-to-many relationship");

            var osBody = (MemberExpression)propertyOtherSide.Body;
            var osProperty = (PropertyInfo)osBody.Member;
            var osHasOwnerAttrib = osProperty.GetCustomAttributes<OwnerSideAttribute>().Count() > 0;
            var osHasInverseAttrib = osProperty.GetCustomAttributes<InverseSideAttribute>().Count() > 0;
            if (osHasOwnerAttrib && osHasInverseAttrib) throw new InvalidOperationException("Only one type of relationship side attribute is allowed on a property");
            if (!osHasOwnerAttrib && !osHasInverseAttrib) throw new InvalidOperationException("Missing attribute for determining relationship side of a many-to-many relationship");

            if ((hasOwnerAttrib == osHasOwnerAttrib) || (hasInverseAttrib == osHasInverseAttrib)) throw new InvalidOperationException("Both sides of the relationship cannot have the same attribute");

            property.SetValue(parent, new Many<TChild>(parent, property.Name, osProperty.Name, hasInverseAttrib));
        }
    }
}
