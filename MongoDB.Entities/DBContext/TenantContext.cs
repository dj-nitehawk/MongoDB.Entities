using MongoDB.Driver;

namespace MongoDB.Entities
{
    /// <summary>
    /// Use this class as the main entrypoint when using multi-tenancy
    /// </summary>
    public class TenantContext : DBContext
    {
        /// <summary>
        /// Initializes a DBContext instance with the given connection parameters.
        /// <para>TIP: network connection is deferred until the first actual operation.</para>
        /// </summary>
        /// <param name="tenantPrefix">A tenant prefix value. Database names for each tenant will have this value prefixed to them</param>
        /// <param name="dbName">Name of the database</param>
        /// <param name="host">Address of the MongoDB server</param>
        /// <param name="port">Port number of the server</param>
        /// <param name="modifiedBy">An optional ModifiedBy instance. 
        /// When supplied, all save/update operations performed via this DBContext instance will set the value on entities that has a property of type ModifiedBy. 
        /// You can even inherit from the ModifiedBy class and add your own properties to it. 
        /// Only one ModifiedBy property is allowed on a single entity type.</param>
        public TenantContext(string tenantPrefix, string dbName, string host = "127.0.0.1", int port = 27017, ModifiedBy modifiedBy = null)
        {
            DB.Initialize(
               new MongoClientSettings { Server = new MongoServerAddress(host, port) },
               $"{tenantPrefix}~{dbName}",
               true)
             .GetAwaiter()
             .GetResult();

            ModifiedBy = modifiedBy;
            this.tenantPrefix = tenantPrefix;
        }

        /// <summary>
        /// Initializes a DBContext instance with the given connection parameters.
        /// <para>TIP: network connection is deferred until the first actual operation.</para>
        /// </summary>
        /// <param name="tenantPrefix">A tenant prefix value. Database names for each tenant will have this value prefixed to them</param>
        /// <param name="dbName">Name of the database</param>
        /// <param name="settings">A MongoClientSettings object</param>
        /// <param name="modifiedBy">An optional ModifiedBy instance. 
        /// When supplied, all save/update operations performed via this DBContext instance will set the value on entities that has a property of type ModifiedBy. 
        /// You can even inherit from the ModifiedBy class and add your own properties to it. 
        /// Only one ModifiedBy property is allowed on a single entity type.</param>
        public TenantContext(string tenantPrefix, string dbName, MongoClientSettings settings, ModifiedBy modifiedBy = null)
        {
            DB.Initialize(settings, $"{tenantPrefix}~{dbName}", true)
              .GetAwaiter()
              .GetResult();

            ModifiedBy = modifiedBy;
            this.tenantPrefix = tenantPrefix;
        }
    }
}
