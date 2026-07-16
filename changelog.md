### BREAKING CHANGES

- **Relationships now work with any entity ID type/representation (dynamic IDs).** `[AsBsonId]` has been removed and join records (`JoinRecord.ParentID`/`ChildID`) are now `BsonValue` properties that hold the exact stored representation of the related entities' `_id` values (string, ObjectId, long, Guid, custom-represented IDs, etc.). `JoinRecord` now derives from `ObjectIdEntity` instead of `Entity`.
- **`[AsObjectId]` is no longer applied by the library to public entity/reference IDs.** The attribute still exists for opt-in use on your own properties, but it has been removed from `Entity.ID`, `One<T>.ID` and `ModifiedBy.UserID`. String IDs on those members are now stored as plain strings unless you map/decorate them yourself (see migration notes — inherited `Entity.ID` cannot be re-attributed; use a class map or implement `IEntity`). Internal `[BINARY_CHUNKS].FileID` still uses `[AsObjectId]` so existing file data keeps matching ObjectId-format parent IDs.
- `Many<TChild,TParent>.ParentsQueryable(...)` now accepts `object`/`IEnumerable<object>` child IDs instead of `string`/`IEnumerable<string>`.
- **Guid IDs:** entities with `Guid` ID properties require a Guid serializer to be registered before `DB.InitAsync`, e.g. `BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));`
- **`HasDefaultID()` instance methods removed** from `Entity`, `ObjectIdEntity` and internal types. Unsaved-entity detection is via the (now public) `HasDefaultID()` extension method, which uses the resolved `IIdGenerator.IsEmpty` when a generator exists (so `string.Empty` and custom empty sentinels regenerate) and otherwise compares the ID to its CLR default. Remove your own implementations/overrides; prior instance overrides were already ignored by the library internally.
- **`GenerateNewID()` removed from `IEntity` and all entity base classes** — `IEntity` is now a marker interface. ID generation is driven by the mongodb driver's `IIdGenerator`, resolved per entity type in this order: a generator registered with the new `DB.RegisterIdGenerator<TEntity>(...)` (works for any entity, including `Entity` subclasses), the generator on the entity's `BsonClassMap`, one registered for the ID's CLR type via `BsonSerializer.RegisterIdGenerator(...)`, then library defaults for `string` (ObjectId-formatted strings, same as before), `ObjectId` and `Guid` IDs. A `GenerateNewID()` extension method replaces the instance method. **If you overrode `GenerateNewID()` for a custom ID format, register an `IIdGenerator` via `DB.RegisterIdGenerator<TEntity>()` instead** — otherwise string IDs will silently receive ObjectId-format values, and IDs of other types without a resolvable generator will throw on save.
- The library's global serializers and conventions now register via a module initializer (previously the `DB` static constructor). `Date`/`FuzzyString`/`decimal` defaults are supplied through an `IBsonSerializationProvider` (not eager `RegisterSerializer`/`TryRegisterSerializer`), so a user-registered serializer for those types wins when registered before the first lookup of the type. Note: `TryRegisterSerializer` throws if a *different* serializer is already registered — it does not let the second caller win.

#### Data migration for existing databases

Databases written by previous versions store ObjectId-parseable string IDs as **BSON ObjectId** in entity `_id` fields, join record `ParentID`/`ChildID` values, `One<T>.ID`/`ModifiedBy.UserID` references, and `[BINARY_CHUNKS].FileID`. After this upgrade, plain string ID properties can no longer read those values and new writes store plain strings. Choose one of:

1. **Keep ObjectId storage for entity `_id`s (recommended when you have data):** you cannot put an attribute on the inherited `Entity.ID` property, so use one of:
   - **Class map** (before first use of the entity type / `DB.InitAsync`):
     ```csharp
     // usings: MongoDB.Bson, MongoDB.Bson.Serialization, MongoDB.Bson.Serialization.Serializers
     BsonClassMap.RegisterClassMap<Book>(cm =>
     {
         cm.AutoMap();
         cm.SetIgnoreExtraElements(true);
         cm.MapIdProperty(b => b.ID)
           .SetSerializer(new StringSerializer(BsonType.ObjectId));
     });
     ```
     Repeat per concrete entity type (or map a custom base type you control).
   - **Own the ID property** by implementing `IEntity` instead of inheriting `Entity`:
     ```csharp
     public class Book : IEntity
     {
         [BsonId, AsObjectId] // or [BsonRepresentation(BsonType.ObjectId)]
         public string ID { get; set; } = null!;
     }
     ```
   Join records follow each entity's stored `_id` representation automatically.
2. **Convert stored ObjectIds to strings**, e.g. in `mongosh` for each affected collection/field:
   ```js
   db.MyCollection.find({ _id: { $type: "objectId" } }).forEach(d => {
     db.MyCollection.insertOne({ ...d, _id: String(d._id) });
     db.MyCollection.deleteOne({ _id: d._id });
   });
   // also convert join collections ([Parent~Child(Prop)]) ParentID/ChildID,
   // One<T> / ModifiedBy.UserID reference fields, and [BINARY_CHUNKS].FileID
   // from objectId to string when those documents still store ObjectIds.
   ```

**Always convert (or accept string storage for) library-owned string reference fields:** `One<T>.ID` and `ModifiedBy.UserID` no longer use `[AsObjectId]` and always store/read plain strings.

**File chunks:** `[BINARY_CHUNKS].FileID` still uses `[AsObjectId]`, so ObjectId-format parent IDs keep matching existing chunk docs. File entity metadata `_id` still follows your entity ID mapping (option 1 or 2). If you convert file entity `_id`s to strings, also convert matching `[BINARY_CHUNKS].FileID` values to strings (or keep parent IDs ObjectId-represented so `FileID` continues to store ObjectIds).

### NEW

- One-to-many and many-to-many relationships (add/remove, children count, children/parents queryables and fluents, cascade deletes) now support entities with `string`, custom-format `string`, `long`, `ObjectId`, `Guid` and custom-represented (e.g. `[BsonRepresentation(BsonType.ObjectId)] string`) ID properties. Join records and queries always use the stored representation of the ID, eliminating silent empty results for custom-represented IDs.
- Add `SetMigrationActivator(Func<Type, IMigration>)` method to support custom activation of migration classes.

### IMPROVEMENTS

- Upgrade mongodb driver to latest.

[//]: # (### FIXES)
