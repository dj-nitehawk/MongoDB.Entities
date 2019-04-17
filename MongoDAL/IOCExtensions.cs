using Microsoft.Extensions.DependencyInjection;

namespace MongoDAL
{
    public static class IOCExtensions
    {
        /// <summary>
        /// Registers MongoDB DAL as a service with the IOC services collection.
        /// </summary>
        /// <param name="Database">MongoDB database name</param>
        /// <param name="Host">MongoDB host address. Defaults to 127.0.0.1</param>
        /// <param name="Port">MongoDB port number. Defaults to 27017</param>
        /// <returns></returns>
        public static IServiceCollection AddMongoDAL(
            this IServiceCollection services, 
            string Database, 
            string Host = "127.0.0.1", 
            string Port = "27017")
        {
            services.AddSingleton<DB>(new DB(Database, Host, Port));
            return services;
        }
    }
}
