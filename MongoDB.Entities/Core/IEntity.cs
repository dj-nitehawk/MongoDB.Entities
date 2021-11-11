namespace MongoDB.Entities;

/// <summary>
/// The contract for Entity classes
/// </summary>
public interface IEntity : IEntity<string>
{

}

/// <summary>
/// The contract for Entity classes
/// </summary>
public interface IEntity<TId>
    where TId : IComparable<TId>, IEquatable<TId>
{
    /// <summary>
    /// The ID property for this entity type.
    /// <para>IMPORTANT: make sure to decorate this property with the [BsonId] attribute when implementing this interface</para>
    /// </summary>
    TId? ID { get; set; }

    /// <summary>
    /// Generate and return a new ID string from this method. It will be used when saving new entities that don't have their ID set. 
    /// That is, if an entity has a null ID, this method will be called for getting a new ID value. 
    /// If you're not doing custom ID generation, simply do <c>return ObjectId.GenerateNewId().ToString()</c>
    /// </summary>
    TId GenerateNewID();
}

//public class MultipleIdEntity : IEntity<ValueTuple<Guid, ObjectId>>
//{
//    public (Guid, ObjectId) ID { get; set; }
//    [BsonIgnore]
//    public Guid Id1
//    {
//        get => ID.Item1;
//        set => ID = (value, ID.Item2);
//    }
//    [BsonIgnore]
//    public ObjectId Id2
//    {
//        get => ID.Item2;
//        set => ID = (ID.Item1, value);
//    }
//    public (Guid, ObjectId) GenerateNewID() => (Guid.NewGuid(), ObjectId.GenerateNewId());
//}