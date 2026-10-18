using System;
using MongoDB.Bson;

namespace MongoDB.Entities;

// ReSharper disable once InconsistentNaming
public partial class DB
{
    /// <summary>
    /// Returns a DataStreamer object to enable uploading/downloading file data directly by supplying the ID of the file entity
    /// </summary>
    /// <typeparam name="T">The file entity type</typeparam>
    /// <param name="ID">The ID of the file entity</param>
    public DataStreamer<T> File<T>(string ID) where T : FileEntity<T>, new()
        => new(new T { ID = ID }, this);
}