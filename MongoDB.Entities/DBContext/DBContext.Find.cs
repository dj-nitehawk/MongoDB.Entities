namespace MongoDB.Entities
{
    public partial class DBContext
    {
        /// <summary>
        /// Starts a find command for the given entity type
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        public Find<T> Find<T>() where T : IEntity
        {
            return new Find<T>(Session, _globalFilters, tenantPrefix);
        }

        /// <summary>
        /// Starts a find command with projection support for the given entity type
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TProjection">The type of the end result</typeparam>
        public Find<T, TProjection> Find<T, TProjection>() where T : IEntity
        {
            return new Find<T, TProjection>(Session, _globalFilters, tenantPrefix);
        }
    }
}
