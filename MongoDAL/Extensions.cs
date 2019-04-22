using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MongoDAL
{
    public static class Extensions
    {
        internal static void ThrowIfUnsaved(this Entity entity)
        {
            if (string.IsNullOrEmpty(entity.ID)) throw new InvalidOperationException("Please save the entity before performing this operation!");
        }

        /// <summary>
        /// Registers MongoDB DAL as a service with the IOC services collection.
        /// </summary>
        /// <param name="Database">MongoDB database name.</param>
        /// <param name="Host">MongoDB host address. Defaults to 127.0.0.1</param>
        /// <param name="Port">MongoDB port number. Defaults to 27017</param>
        /// <returns></returns>
        public static IServiceCollection AddMongoDAL(
            this IServiceCollection services,
            string Database,
            string Host = "127.0.0.1",
            int Port = 27017)
        {
            services.AddSingleton<DB>(new DB(Database, Host, Port));
            return services;
        }

        /// <summary>
        /// Registers MongoDB DAL as a service with the IOC services collection.
        /// </summary>
        /// <param name="Settings">A 'MongoClientSettings' object with customized connection parameters such as authentication credentials.</param>
        /// <param name="Database">MongoDB database name.</param>
        /// <returns></returns>
        public static IServiceCollection AddMongoDAL(
            this IServiceCollection services,
            MongoClientSettings Settings,
            string Database)
        {
            services.AddSingleton<DB>(new DB(Settings, Database));
            return services;
        }

        /// <summary>
        /// Returns a reference to this entity.
        /// </summary>
        public static One<T> ToReference<T>(this T entity) where T : Entity
        {
            return new One<T>(entity);
        }

        /// <summary>
        /// Initializes a new reference collection.
        /// </summary>
        /// <param name="parent">The parent Entity needed to initialize the collection.</param>
        public static Many<TParent, TChild> Initialize<TParent, TChild>(this Many<TParent, TChild> refmany, TParent parent) where TParent : Entity where TChild : Entity
        {
            return new Many<TParent, TChild>(parent);
        }

        /// <summary>
        /// Replaces an Entity in the databse if a matching item is found (by ID) or creates a new one if not found.
        /// <para>WARNING:</para>
        /// <para>The shape of the Entity in the database is always owerwritten with the current shape of the Entity. So be mindful of data loss due to schema changes.</para>
        /// </summary>
        public static void Save<T>(this T entity) where T : Entity
        {
            SaveChangesAsync<T>(entity).Wait();
        }

        /// <summary>
        /// Replaces an Entity in the databse if a matching item is found (by ID) or creates a new one if not found.
        /// <para>WARNING:</para>
        /// <para>The shape of the Entity in the database is always owerwritten with the current shape of the Entity. So be mindful of data loss due to schema changes.</para>
        /// </summary>
        public static Task SaveChangesAsync<T>(this T entity) where T : Entity
        {
            return DB.SaveAsync<T>(entity);
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many relationships, those references are also deleted.</para>
        /// </summary>
        public static void Delete<T>(this T entity) where T : Entity
        {
            DeleteAsync<T>(entity).Wait();
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many relationships, those references are also deleted.</para>
        /// </summary>
        public static Task DeleteAsync<T>(this T entity) where T : Entity
        {
            return DB.DeleteAsync<T>(entity.ID);
        }

        /// <summary>
        /// Deletes multiple entities from the database
        /// <para>HINT: If these entities are referenced by one-to-many relationships, those references are also deleted.</para>
        /// </summary>
        public static void DeleteAll<T>(this IEnumerable<T> entities) where T : Entity
        {
            DeleteAllAsync<T>(entities).Wait();
            entities = null;
        }

        /// <summary>
        /// Deletes multiple entities from the database
        /// <para>HINT: If these entities are referenced by one-to-many relationships, those references are also deleted.</para>
        /// </summary>
        public static Task DeleteAllAsync<T>(this IEnumerable<T> entities) where T : Entity
        {
            var tasks = new List<Task>();

            foreach (var e in entities)
            {
                tasks.Add(DB.DeleteAsync<T>(e.ID));
            }

            return Task.WhenAll(tasks);
        }
    }


}
