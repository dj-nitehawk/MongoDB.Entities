using MongoDB.Driver;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// Use this class as the main entrypoint when using multi-tenancy
    /// </summary>
    public class TenantContext : DBContext
    {
        /// <summary>
        /// Instantiate a tenant context with the given tenant prefix value.
        /// </summary>
        /// <param name="tenantPrefix">The tenant prefix to be prepended to database names.</param>
        public TenantContext(string tenantPrefix)
        {
            this.tenantPrefix = tenantPrefix;
        }

        /// <summary>
        /// Configure this tenant context to be able to connect to a particular database/server.
        /// <para>TIP: network connection is deferred until the first actual operation.</para>
        /// </summary>
        /// <param name="dbName">Name of the database (without tenant prefix)</param>
        /// <param name="host">Address of the MongoDB server</param>
        /// <param name="port">Port number of the server</param>
        /// <param name="modifiedBy">An optional ModifiedBy instance. 
        /// When supplied, all save/update operations performed via this DBContext instance will set the value on entities that has a property of type ModifiedBy. 
        /// You can even inherit from the ModifiedBy class and add your own properties to it. 
        /// Only one ModifiedBy property is allowed on a single entity type.</param>
        public Task InitAsync(string dbName, string host = "127.0.0.1", int port = 27017, ModifiedBy modifiedBy = null)
        {
            ModifiedBy = modifiedBy;
            return DB.Initialize(
                new MongoClientSettings { Server = new MongoServerAddress(host, port) },
                $"{tenantPrefix}~{dbName}",
                true);
        }

        /// <summary>
        /// Configure this tenant context to be able to connect to a particular database/server.
        /// <para>TIP: network connection is deferred until the first actual operation.</para>
        /// </summary>
        /// <param name="dbName">Name of the database (without tenant prefix)</param>
        /// <param name="settings">A MongoClientSettings object</param>
        /// <param name="modifiedBy">An optional ModifiedBy instance. 
        /// When supplied, all save/update operations performed via this DBContext instance will set the value on entities that has a property of type ModifiedBy. 
        /// You can even inherit from the ModifiedBy class and add your own properties to it. 
        /// Only one ModifiedBy property is allowed on a single entity type.</param>
        public Task InitAsync(string dbName, MongoClientSettings settings, ModifiedBy modifiedBy = null)
        {
            ModifiedBy = modifiedBy;
            return DB.Initialize(settings, $"{tenantPrefix}~{dbName}", true);
        }
    }
}
