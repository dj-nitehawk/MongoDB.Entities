using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver.Linq;
using MongoDB.Bson.Serialization;
using System.Linq.Expressions;
using System.Reflection;
using System.Linq;

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
        /// Registers MongoDB.Entities as a service with the IOC services collection.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="Database">MongoDB database name.</param>
        /// <param name="Host">MongoDB host address. Defaults to 127.0.0.1</param>
        /// <param name="Port">MongoDB port number. Defaults to 27017</param>
        /// <returns></returns>
        public static IServiceCollection AddMongoDBEntities(
            this IServiceCollection services,
            string Database,
            string Host = "127.0.0.1",
            int Port = 27017)
        {
            services.AddSingleton<DB>(new DB(Database, Host, Port));
            return services;
        }

        /// <summary>
        /// Registers MongoDB.Entities as a service with the IOC services collection.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="Settings">A 'MongoClientSettings' object with customized connection parameters such as authentication credentials.</param>
        /// <param name="Database">MongoDB database name.</param>
        /// <returns></returns>
        public static IServiceCollection AddMongoDBEntities(
            this IServiceCollection services,
            MongoClientSettings Settings,
            string Database)
        {
            services.AddSingleton<DB>(new DB(Settings, Database));
            return services;
        }

        /// <summary>
        /// An IQueryable collection of Entities.
        /// </summary>
        public static IMongoQueryable<T> Collection<T>(this T entity) where T : Entity
        {
            return DB.Collection<T>();
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
        async public static Task SaveAsync<T>(this T entity) where T : Entity
        {
            await DB.SaveAsync<T>(entity);
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        public static void Delete<T>(this T entity) where T : Entity
        {
            DeleteAsync<T>(entity).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        async public static Task DeleteAsync<T>(this T entity) where T : Entity
        {
            await DB.DeleteAsync<T>(entity.ID);
        }

        /// <summary>
        /// Deletes multiple entities from the database
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        public static void DeleteAll<T>(this IEnumerable<T> entities) where T : Entity
        {
            DeleteAllAsync<T>(entities).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes multiple entities from the database
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        async public static Task DeleteAllAsync<T>(this IEnumerable<T> entities) where T : Entity
        {
            var tasks = new List<Task>();

            foreach (var e in entities)
            {
                tasks.Add(DB.DeleteAsync<T>(e.ID));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Initializes supplied property with a new One-To-Many reference collection.
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
        /// Initializes supplied property with a new Many-To-Many reference collection.
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
