using System;
using System.Runtime.CompilerServices;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Entities;

/// <summary>
/// Registers the library's global serializers and conventions via a module initializer, which the runtime executes
/// before any code in this assembly runs. Library serializers are supplied through an
/// <see cref="IBsonSerializationProvider"/> so a user-registered serializer for the same type (registered before the
/// first lookup of that type) wins instead of throwing. Eager <c>TryRegisterSerializer</c> is not used because it
/// throws when a different serializer is already registered.
/// </summary>
static class LibraryInitializer
{
    [ModuleInitializer]
    internal static void Init()
    {
        BsonSerializer.RegisterSerializationProvider(new LibrarySerializationProvider());

        ConventionRegistry.Register(
            "DefaultConventions",
            new ConventionPack
            {
                new IgnoreExtraElementsConvention(true),
                new IgnoreManyPropsConvention()
            },
            _ => true);
    }

    sealed class LibrarySerializationProvider : IBsonSerializationProvider
    {
        public IBsonSerializer? GetSerializer(Type type)
        {
            if (type == typeof(Date))
                return new DateSerializer();

            if (type == typeof(FuzzyString))
                return new FuzzyStringSerializer();

            if (type == typeof(decimal))
                return new DecimalSerializer(BsonType.Decimal128);

            if (type == typeof(decimal?))
                return new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128));

            return null;
        }
    }
}
