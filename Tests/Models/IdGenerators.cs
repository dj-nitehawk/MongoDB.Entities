using System;
using System.Threading;
using MongoDB.Bson.Serialization;

namespace MongoDB.Entities.Tests.Models;

// custom IIdGenerators exercising the ID generation extension points.
// they are hooked up to entities/ID types in InitTest.Init.

/// <summary>
/// generates string IDs in a custom format: "{prefix}-{guid}"
/// </summary>
public class PrefixedStringIdGenerator(string prefix) : IIdGenerator
{
    public object GenerateId(object container, object document)
        => $"{prefix}-{Guid.NewGuid():N}";

    public bool IsEmpty(object id)
        => string.IsNullOrEmpty(id as string);
}

/// <summary>
/// generates string IDs in the format "{guid}-{ticks}"
/// </summary>
public class CustomerIdGenerator : IIdGenerator
{
    public object GenerateId(object container, object document)
        => $"{Guid.NewGuid()}-{DateTime.UtcNow.Ticks}";

    public bool IsEmpty(object id)
        => string.IsNullOrEmpty(id as string);
}

/// <summary>
/// generates sequential long IDs
/// </summary>
public class SequentialLongIdGenerator : IIdGenerator
{
    static long _counter = DateTime.UtcNow.Ticks;

    public object GenerateId(object container, object document)
        => Interlocked.Increment(ref _counter);

    public bool IsEmpty(object id)
        => id is not long value || value == 0;
}

/// <summary>
/// generates string IDs from the current UTC tick count
/// </summary>
public class TicksIdGenerator : IIdGenerator
{
    public object GenerateId(object container, object document)
        => DateTime.UtcNow.Ticks.ToString();

    public bool IsEmpty(object id)
        => string.IsNullOrEmpty(id as string);
}

/// <summary>
/// always generates the same ID for provoking duplicate key errors
/// </summary>
public class DuplicateIdGenerator : IIdGenerator
{
    public object GenerateId(object container, object document)
        => "iamnotauniqueid";

    public bool IsEmpty(object id)
        => string.IsNullOrEmpty(id as string);
}


/// <summary>
/// treats the sentinel value "EMPTY" (and null/blank) as empty; generates prefixed IDs.
/// used to verify HasDefaultID honors IIdGenerator.IsEmpty for custom empty sentinels.
/// </summary>
public class SentinelStringIdGenerator : IIdGenerator
{
    public const string EmptySentinel = "EMPTY";

    public object GenerateId(object container, object document)
        => $"sentinel-{Guid.NewGuid():N}";

    public bool IsEmpty(object id)
        => id is not string value || string.IsNullOrEmpty(value) || value == EmptySentinel;
}
