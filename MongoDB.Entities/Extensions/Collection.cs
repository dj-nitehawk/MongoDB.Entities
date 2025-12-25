using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.Entities;

public static partial class Extensions
{
    /// <summary>
    /// Drops a join collection
    /// </summary>
    /// <param name="collection"></param>
    public static Task DropAsync(this IMongoCollection<JoinRecord> collection)
        => collection.Database.DropCollectionAsync(collection.CollectionNamespace.CollectionName);
}