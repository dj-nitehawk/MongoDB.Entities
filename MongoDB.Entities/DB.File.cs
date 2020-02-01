using MongoDB.Bson;
using System;

namespace MongoDB.Entities
{
    public partial class DB
    {
        /// <summary>
        /// Returns a DataStreamer object to enable downloading file data directly by supplying the ID of the file entity
        /// </summary>
        /// <typeparam name="T">The file entity type</typeparam>
        /// <param name="ID">The ID of the file entity</param>
        public static DataStreamer File<T>(string ID) where T : FileEntity, new()
        {
            if (!ObjectId.TryParse(ID, out _))
                throw new ArgumentException("The ID passed in is not of the correct format!");

            return new DataStreamer(new T() { ID = ID, UploadSuccessful = true });
        }

        /// <summary>
        /// Returns a DataStreamer object to enable downloading file data directly by supplying the ID of the file entity
        /// </summary>
        /// <typeparam name="T">The file entity type</typeparam>
        /// <param name="ID">The ID of the file entity</param>
        public DataStreamer File<T>(string ID, string db = null) where T : FileEntity
        {
            return File<T>(ID);
        }
    }
}
