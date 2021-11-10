using MongoDB.Bson;
using MongoDB.Driver;
using System;

namespace MongoDB.Entities;

public partial class DBContext
{
    /// <summary>
    /// Returns a DataStreamer object to enable uploading/downloading file data directly by supplying the ID of the file entity
    /// </summary>
    /// <typeparam name="T">The file entity type</typeparam>
    /// <param name="ID">The ID of the file entity</param>
    /// <param name="collectionName"></param>
    /// <param name="collection"></param>
    public DataStreamer<T> File<T>(string ID, string? collectionName = null, IMongoCollection<T>? collection = null) where T : FileEntity, new()
    {
        if (!ObjectId.TryParse(ID, out _))
            throw new ArgumentException("The ID passed in is not of the correct format!");

        return new DataStreamer<T>(new T() { ID = ID, UploadSuccessful = true }, this, Collection<T>(collectionName, collection));
    }
}
