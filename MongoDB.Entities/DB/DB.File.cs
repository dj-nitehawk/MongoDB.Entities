using MongoDB.Bson;
using System;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        /// <summary>
        /// Returns a DataStreamer object to enable uploading/downloading file data directly by supplying the ID of the file entity
        /// </summary>
        /// <typeparam name="T">The file entity type</typeparam>
        /// <param name="ID">The ID of the file entity</param>
        /// <param name="tenantPrefix">Optional tenant prefix if using multi-tenancy</param>
        public static DataStreamer File<T>(string ID, string tenantPrefix = null) where T : FileEntity, new()
        {
            if (!ObjectId.TryParse(ID, out _))
                throw new ArgumentException("The ID passed in is not of the correct format!");

            return new DataStreamer(
                new T() { ID = ID, UploadSuccessful = true },
                tenantPrefix);
        }
    }
}
