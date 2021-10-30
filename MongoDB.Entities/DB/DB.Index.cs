namespace MongoDB.Entities
{
    public static partial class DB
    {
        /// <summary>
        /// Represents an index for a given IEntity
        /// <para>TIP: Define the keys first with .Key() method and finally call the .Create() method.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static Index<T> Index<T>(string tenantPrefix = null) where T : IEntity
        {
            return new Index<T>(tenantPrefix);
        }
    }
}
