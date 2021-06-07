namespace MongoDB.Entities
{
    public partial class DBContext
    {
        /// <summary>
        /// Starts a replace command for the given entity type
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        public virtual Replace<T> Replace<T>() where T : IEntity
        {
            ThrowIfModifiedByIsEmpty<T>();
            return new Replace<T>(session, ModifiedBy);
        }

        //public virtual UpdateAndGet<T> UpdateAndGet<T>() where T : IEntity
        //{
        //    var upGet = new UpdateAndGet<T>(session);
        //    if (Cache<T>.ModifiedByProp != null)
        //    {
        //        ThrowIfModifiedByIsEmpty<T>();
        //        upGet.Modify(b => b.Set(Cache<T>.ModifiedByProp.Name, ModifiedBy));
        //    }
        //    return upGet;
        //}


        //public virtual UpdateAndGet<T, TProjection> UpdateAndGet<T, TProjection>() where T : IEntity
        //{
        //    var upGet = new UpdateAndGet<T, TProjection>(session);
        //    if (Cache<T>.ModifiedByProp != null)
        //    {
        //        ThrowIfModifiedByIsEmpty<T>();
        //        upGet.Modify(b => b.Set(Cache<T>.ModifiedByProp.Name, ModifiedBy));
        //    }
        //    return upGet;
        //}
    }
}
