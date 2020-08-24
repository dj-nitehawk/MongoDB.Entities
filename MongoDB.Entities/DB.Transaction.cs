using MongoDB.Driver;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        /// <summary>
        /// Gets a transaction context/scope for a given database or the default database if not specified.
        /// </summary>
        /// <param name="database">The name of the database which this transaction is for (not required)</param>
        /// <param name="options">Client session options (not required)</param>
        public static Transaction Transaction(string database = default, ClientSessionOptions options = null)
        {
            return new Transaction(database, options);
        }

        /// <summary>
        /// Gets a transaction context/scope for a given entity type's database
        /// </summary>
        /// <typeparam name="T">The entity type to determine the database from for the transaction</typeparam>
        /// <param name="options">Client session options (not required)</param>
        public static Transaction Transaction<T>(ClientSessionOptions options = null) where T : IEntity
        {
            return new Transaction(DatabaseName<T>(), options);
        }
    }
}
