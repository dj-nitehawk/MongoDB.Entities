using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System.Collections.Generic;

namespace MongoDAL
{
    public static class Extensions
    {
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
        public static MongoRef<T> CreateRef<T>(this T entity) where T : MongoEntity
        {
            return new MongoRef<T>(entity);
        }

        //todo: remarks
        public static MongoRefs<T> CreateRefs<T>(this T entity) where T : MongoEntity
        {
            return new MongoRefs<T>(entity);
        }

        //todo: remarks
        public static MongoRefs<T> CreateRefs<T>(this IEnumerable<T> entities) where T : MongoEntity
        {
            return new MongoRefs<T>(entities);
        }

        //tood: test extensions for all methods.
        public static void Save<T>(this T entity) where T : MongoEntity
        {
            DB.Save<T>(entity);
        }
    }
}
