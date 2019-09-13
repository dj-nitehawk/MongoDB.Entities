using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

        internal static void ThrowIfUnsaved(this Entity entity)
        {
            if (string.IsNullOrEmpty(entity.ID)) throw new InvalidOperationException("Please save the entity before performing this operation!");
        }

        /// <summary>
        /// Gets the name of the database this entity is attached to. Returns null if not attached.
        /// </summary>
        public static string Database(this Entity entity)
        {
            var attribute = entity.GetType().GetCustomAttribute<DatabaseAttribute>();
            if (attribute != null)
            {
                return attribute.Name;
            }
            return null;
        }

        /// <summary>
        /// Returns the full dotted path of a property for the given expression
        /// </summary>
        /// <typeparam name="T">Any class that inherits from Entity</typeparam>
        public static string FullPath<T>(this Expression<Func<T, object>> expression)
        {
            if (expression == null) return null;
            var name = expression.Parameters[0].Name;
            return expression.ToString()
                       .Replace($"{name} => {name}.", "")
                       .Replace($"{name} => Convert({name}.", "")
                       .Replace(", Object)", "")
                       .Replace("get_Item(-1).", "")
                       .Replace("[-1]", "");
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
        /// Gets the IMongoCollection for a given Entity type.
        /// <para>TIP: Try never to use this unless really neccessary.</para>
        /// </summary>
        /// <typeparam name="T">Any class that inherits from Entity</typeparam>
        public static IMongoCollection<T> Collection<T>(this T Entity) where T : Entity
        {
            return DB.Collection<T>();
        }

        /// <summary>
        /// An IQueryable collection of sibling Entities.
        /// </summary>
        public static IMongoQueryable<T> Queryable<T>(this T entity, AggregateOptions options = null) where T : Entity
        {
            return DB.Queryable<T>(options);
        }

        /// <summary>
        /// An IAggregateFluent collection of sibling Entities.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        public static IAggregateFluent<T> Fluent<T>(this T entity, IClientSessionHandle session = null, AggregateOptions options = null) where T : Entity
        {
            return DB.Fluent<T>(options, session);
        }

        /// <summary>
        /// Adds a distinct aggregation stage to a fluent pipeline. 
        /// </summary>
        /// <typeparam name="T">Any class that inherits from Entity</typeparam>
        public static IAggregateFluent<T> Distinct<T>(this IAggregateFluent<T> aggregate) where T : Entity
        {
            PipelineStageDefinition<T, T> groupStage =
                new BsonDocument(
                "$group", new BsonDocument()
                          .Add("_id", "$_id")
                          .Add("doc", new BsonDocument()
                                      .Add("$first", "$$ROOT")));

            PipelineStageDefinition<T, T> rootStage =
                new BsonDocument("$replaceRoot", new BsonDocument()
                                                 .Add("newRoot", "$doc"));

            return aggregate.AppendStage(groupStage).AppendStage(rootStage);
        }

        /// <summary>
        /// Appends a match stage to the pipeline with a filter expression
        /// </summary>
        /// <typeparam name="T">Any class that inherits from Entity</typeparam>
        /// <param name="aggregate"></param>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        public static IAggregateFluent<T> Match<T>(this IAggregateFluent<T> aggregate, Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter) where T : Entity
        {
            return aggregate.Match(filter(Builders<T>.Filter));
        }

        /// <summary>
        /// Returns a reference to this entity.
        /// </summary>
        public static One<T> ToReference<T>(this T entity) where T : Entity
        {
            return new One<T>(entity);
        }

        /// <summary>
        /// Creates an unlinked duplicate of the original Entity ready for embedding with a blank ID.
        /// </summary>
        public static T ToDocument<T>(this T entity) where T : Entity
        {
            var res = entity.Duplicate();
            res.ID = ObjectId.Empty.ToString();
            return res;
        }

        /// <summary>
        /// Creates unlinked duplicates of the original Entities ready for embedding with blank IDs.
        /// </summary>
        public static T[] ToDocuments<T>(this T[] entities) where T : Entity
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
        public static IEnumerable<T> ToDocuments<T>(this IEnumerable<T> entities) where T : Entity
        {
            var res = entities.Duplicate();
            foreach (var e in res)
            {
                e.ID = ObjectId.Empty.ToString();
            }
            return res;
        }

        /// <summary>
        /// Replaces an Entity in the databse if a matching item is found (by ID) or creates a new one if not found.
        /// <para>WARNING: The shape of the Entity in the database is always owerwritten with the current shape of the Entity. So be mindful of data loss due to schema changes.</para>
        /// </summary>
        public static void Save<T>(this T entity) where T : Entity
        {
            SaveAsync<T>(entity).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Replaces an Entity in the databse if a matching item is found (by ID) or creates a new one if not found.
        /// <para>WARNING: The shape of the Entity in the database is always owerwritten with the current shape of the Entity. So be mindful of data loss due to schema changes.</para>
        /// </summary>
        public static async Task SaveAsync<T>(this T entity) where T : Entity
        {
            await DB.SaveAsync(entity);
        }

        /// <summary>
        /// Replaces Entities in the databse if matching items are found (by ID) or creates new ones if not found.
        /// <para>WARNING: The shape of the Entity in the database is always owerwritten with the current shape of the Entity. So be mindful of data loss due to schema changes.</para>
        /// </summary>
        public static void Save<T>(this IEnumerable<T> entities) where T : Entity
        {
            SaveAsync(entities).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Replaces Entities in the databse if matching items are found (by ID) or creates new ones if not found.
        /// <para>WARNING: The shape of the Entity in the database is always owerwritten with the current shape of the Entity. So be mindful of data loss due to schema changes.</para>
        /// </summary>
        public static async Task SaveAsync<T>(this IEnumerable<T> entities) where T : Entity
        {
            await DB.SaveAsync(entities);
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        public static void Delete<T>(this T entity) where T : Entity
        {
            DeleteAsync(entity).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        public static async Task DeleteAsync<T>(this T entity) where T : Entity
        {
            await DB.DeleteAsync<T>(entity.ID);
        }

        /// <summary>
        /// Deletes multiple entities from the database
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        public static void DeleteAll<T>(this IEnumerable<T> entities) where T : Entity
        {
            DeleteAllAsync(entities).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes multiple entities from the database
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        public static async Task DeleteAllAsync<T>(this IEnumerable<T> entities) where T : Entity
        {
            await DB.DeleteAsync<T>(entities.Select(e => e.ID));
        }

        /// <summary>
        /// Initializes supplied property with a new One-To-Many relationship.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="propertyToInit">() => PropertyName</param>
        public static void InitOneToMany<TChild>(this Entity parent, Expression<Func<Many<TChild>>> propertyToInit) where TChild : Entity
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
        public static void InitManyToMany<TChild>(this Entity parent, Expression<Func<Many<TChild>>> propertyToInit, Expression<Func<TChild, object>> propertyOtherSide) where TChild : Entity
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
