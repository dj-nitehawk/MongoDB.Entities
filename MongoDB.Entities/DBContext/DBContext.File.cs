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
    /// <typeparam name="TId">ID type</typeparam>
    /// <param name="ID">The ID of the file entity</param>
    /// <param name="collectionName"></param>
    /// <param name="collection"></param>
    public DataStreamer<T, TId> File<T, TId>(TId ID, string? collectionName = null, IMongoCollection<T>? collection = null)
        where TId : IComparable<TId>, IEquatable<TId>
        where T : FileEntity<TId>, new()
    {
        return new DataStreamer<T, TId>(new T() { ID = ID, UploadSuccessful = true }, this, Collection(collectionName, collection));
    }
}
