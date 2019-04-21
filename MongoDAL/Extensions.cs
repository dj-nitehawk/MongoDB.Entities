using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

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
        public static RefOne<T> ToReference<T>(this T entity) where T : Entity
        {
            return new RefOne<T>(entity);
        }
        
        //tood: test extensions for all methods.
        public static void SaveToDB<T>(this T entity) where T : Entity
        {
            DB.Save<T>(entity);
        }
    }
}
