using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.Entities;

static class FileMetadata
{
    internal static readonly string[] ManagedProperties = ["BinaryStorageID", "FileSize", "ChunkCount", "UploadSuccessful"];

    internal static string ManagedMemberName(BsonClassMap classMap, string memberName)
        => classMap.AllMemberMaps.First(m => m.MemberName == memberName).ElementName;

    internal static UpdateDefinition<T> SaveUpdate<T>(T entity) where T : IEntity
    {
        var classMap = BsonClassMap.LookupClassMap(entity.GetType());
        var fields = entity.ToBsonDocument();
        var idName = classMap.IdMemberMap.ElementName;
        fields.Remove(idName);
        var preserved = new BsonDocument(idName, "$" + idName);
        var defaults = new BsonDocument
        {
            { ManagedMemberName(classMap, "BinaryStorageID"), BsonNull.Value },
            { ManagedMemberName(classMap, "FileSize"), 0L },
            { ManagedMemberName(classMap, "ChunkCount"), 0 },
            { ManagedMemberName(classMap, "UploadSuccessful"), false }
        };
        foreach (var field in defaults)
        {
            fields.Remove(field.Name);
            preserved[field.Name] = new BsonDocument("$ifNull", new BsonArray { "$" + field.Name, field.Value });
        }

        // Replace application metadata atomically, retaining the current upload even for stale entities.
        // Treat serialized values as literals, not aggregation expressions or field references.
        return new PipelineUpdateDefinition<T>(new[]
        {
            new BsonDocument("$replaceWith", new BsonDocument("$mergeObjects", new BsonArray
            {
                new BsonDocument("$literal", fields),
                preserved
            }))
        });
    }
}
