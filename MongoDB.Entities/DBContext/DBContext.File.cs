namespace MongoDB.Entities
{
    public partial class DBContext
    {
        /// <summary>
        /// Returns a DataStreamer object to enable uploading/downloading file data directly by supplying the ID of the file entity
        /// </summary>
        /// <typeparam name="T">The file entity type</typeparam>
        /// <param name="ID">The ID of the file entity</param>
        public DataStreamer File<T>(string ID) where T : FileEntity, new()
        {
            return DB.File<T>(ID, tenantPrefix);
        }
    }
}
