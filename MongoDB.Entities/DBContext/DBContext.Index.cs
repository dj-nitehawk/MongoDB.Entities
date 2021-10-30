namespace MongoDB.Entities
{
    public partial class DBContext
    {
        /// <summary>
        /// Represents an index for a given IEntity
        /// <para>TIP: Define the keys first with .Key() method and finally call the .Create() method.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public Index<T> Index<T>() where T : IEntity
        {
            return new Index<T>(tenantPrefix);
        }
    }
}
