using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
#nullable enable
namespace MongoDB.Entities
{
    internal interface ICollectionRelated<T> where T : IEntity
    {
        public DBContext Context { get; }
        public IMongoCollection<T> Collection { get; }
    }
    internal static class ICollectionRelatedExt
    {
        public static IClientSessionHandle? Session<T>(this ICollectionRelated<T> collectionRelated) where T : IEntity
        {
            return collectionRelated.Context.Session;
        }

    }
}
