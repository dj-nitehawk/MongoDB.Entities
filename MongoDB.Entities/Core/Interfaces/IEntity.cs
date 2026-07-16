namespace MongoDB.Entities;

/// <summary>
/// The marker contract for Entity classes. Implementing types must have an ID property recognizable by the
/// mongodb driver as the primary key ('_id', 'Id', 'ID', or a property decorated with the [BsonId] attribute).
/// <para>
/// When saving new entities whose ID property holds the default value of its type (null for strings, ObjectId.Empty,
/// 0, Guid.Empty, etc.), a new ID is generated via the IIdGenerator resolved for the ID property: one registered for
/// the entity type with DB.RegisterIdGenerator&lt;TEntity&gt;() (highest precedence), the generator set on the entity's
/// BsonClassMap (cm.IdMemberMap.SetIdGenerator()), one registered for the ID's CLR type with
/// BsonSerializer.RegisterIdGenerator(), or the library defaults for string (ObjectId format), ObjectId and Guid IDs.
/// </para>
/// </summary>
public interface IEntity;