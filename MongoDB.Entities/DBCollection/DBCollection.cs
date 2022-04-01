using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public partial class DBCollection<T> : IMongoCollection<T>
{    
    public IMongoCollection<T> Collection { get; }

    public DBCollection(IMongoCollection<T> collection)
    {
        Collection = collection;
    }
}
