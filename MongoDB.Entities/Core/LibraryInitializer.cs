using System.Runtime.CompilerServices;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Entities;

/// <summary>
/// Registers the library's global serializers and conventions via a module initializer, which the runtime executes
/// before any code in this assembly runs. That way user code is free to register class maps, serializers and id
/// generators in any order before (or after) DB.InitAsync() without racing the library's own registrations.
/// </summary>
static class LibraryInitializer
{
    [ModuleInitializer]
    internal static void Init()
    {
        BsonSerializer.TryRegisterSerializer(new DateSerializer());
        BsonSerializer.TryRegisterSerializer(new FuzzyStringSerializer());
        BsonSerializer.TryRegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
        BsonSerializer.TryRegisterSerializer(typeof(decimal?), new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));

        ConventionRegistry.Register(
            "DefaultConventions",
            new ConventionPack
            {
                new IgnoreExtraElementsConvention(true),
                new IgnoreManyPropsConvention()
            },
            _ => true);
    }
}
