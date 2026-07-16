### BREAKING CHANGES

- **Relationships now work with any entity ID type/representation (dynamic IDs).** `[AsBsonId]` has been removed and join records (`JoinRecord.ParentID`/`ChildID`) are now `BsonValue` properties that hold the exact stored representation of the related entities' `_id` values (string, ObjectId, long, Guid, custom-represented IDs, etc.). `JoinRecord` now derives from `ObjectIdEntity` instead of `Entity`.
- **`[AsObjectId]` is no longer applied by the library to public entity/reference IDs.** The attribute still exists for opt-in use on your own properties, but it has been removed from `Entity.ID`, `One<T>.ID` and `ModifiedBy.UserID`. String IDs on those members are now stored as plain strings unless you map/decorate them yourself (see migration notes — inherited `Entity.ID` cannot be re-attributed; use a class map or implement `IEntity`). Internal `[BINARY_CHUNKS].FileID` still uses `[AsObjectId]` so existing file data keeps matching ObjectId-format parent IDs.
- `Many<TChild,TParent>.ParentsQueryable(...)` now accepts `object`/`IEnumerable<object>` child IDs instead of `string`/`IEnumerable<string>`, plus `IReadOnlyList<TId> where TId : struct` for value-type ID arrays.
- **Guid IDs:** entities with `Guid` ID properties require a Guid serializer to be registered before `DB.InitAsync`, e.g. `BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));`
- **`HasDefaultID()` instance methods removed** from `Entity`, `ObjectIdEntity` and internal types. Unsaved-entity detection is via the (now public) `HasDefaultID()` extension method, which uses the resolved `IIdGenerator.IsEmpty` when a generator exists (so `string.Empty` and custom empty sentinels regenerate) and otherwise compares the ID to its CLR default. Remove your own implementations/overrides; prior instance overrides were already ignored by the library internally.
- **`GenerateNewID()` removed from `IEntity` and all entity base classes** — `IEntity` is now a marker interface. ID generation is driven by the mongodb driver's `IIdGenerator`, resolved per entity type in this order: a generator registered with the new `DB.RegisterIdGenerator<TEntity>(...)` (works for any entity, including `Entity` subclasses), the generator on the entity's `BsonClassMap`, one registered for the ID's CLR type via `BsonSerializer.RegisterIdGenerator(...)`, then library defaults for `string` (ObjectId-formatted strings, same as before), `ObjectId` and `Guid` IDs. A `GenerateNewID()` extension method replaces the instance method. **If you overrode `GenerateNewID()` for a custom ID format, register an `IIdGenerator` via `DB.RegisterIdGenerator<TEntity>()` instead** — otherwise string IDs will silently receive ObjectId-format values, and IDs of other types without a resolvable generator will throw on save.
- The library's global serializers and conventions now register via a module initializer (previously the `DB` static constructor). `Date`/`FuzzyString`/`decimal` defaults are supplied through an `IBsonSerializationProvider` (not eager `RegisterSerializer`/`TryRegisterSerializer`), so a user-registered serializer for those types wins when registered before the first lookup of the type. Note: `TryRegisterSerializer` throws if a *different* serializer is already registered — it does not let the second caller win.

#### Data migration for existing databases

Databases written by previous versions store ObjectId-parseable string IDs as **BSON ObjectId** in entity `_id` fields, join record `ParentID`/`ChildID` values, `One<T>.ID`/`ModifiedBy.UserID` references, embedded `Entity` IDs, and `[BINARY_CHUNKS].FileID`. After this upgrade, public plain string ID/reference properties can no longer read those values and new writes store plain strings. `[BINARY_CHUNKS].FileID` is the compatibility exception described below. Choose one of:

1. **Keep ObjectId storage for entity `_id`s (recommended when you have data):** you cannot put an attribute on the inherited `Entity.ID` property, and you also cannot remap that inherited member on each concrete type (`BsonClassMap.RegisterClassMap<Book>(...).MapIdProperty(b => b.ID)` throws because `ID` is declared on `Entity`, not `Book`). Use one of:

   - **Class map on the `Entity` base** (before first use of any `Entity` subclass / before `DB.InitAsync`). This affects **every** type that inherits `Entity`, including nested/embedded entities that use the same base:
     ```csharp
     // usings: MongoDB.Bson, MongoDB.Bson.Serialization,
     //         MongoDB.Bson.Serialization.IdGenerators, MongoDB.Bson.Serialization.Serializers
     BsonClassMap.RegisterClassMap<Entity>(cm =>
     {
         cm.AutoMap();
         cm.SetIgnoreExtraElements(true);
         cm.MapIdProperty(e => e.ID)
           .SetSerializer(new StringSerializer(BsonType.ObjectId))
           .SetIdGenerator(StringObjectIdGenerator.Instance);
     });
     ```
   - **Per-entity ObjectId storage** requires owning the ID member yourself via `IEntity` (do not try to re-map inherited `Entity.ID` per concrete type):
     ```csharp
     public class Book : IEntity
     {
         [BsonId, AsObjectId] // or [BsonRepresentation(BsonType.ObjectId)]
         public string ID { get; set; } = null!;
     }
     ```
   Join records follow each entity's stored `_id` representation automatically.

2. **Convert stored ObjectIds to strings** (offline, with backups). Treat this as a controlled, **idempotent** migration: only rewrite documents whose field is still BSON ObjectId (`$type: "objectId"`), and ensure destination string `_id`s cannot collide (unique index on `_id` is automatic; for reference fields, re-check uniqueness if you rely on it). Sketch for a single collection:

   ```js
   // BACK UP FIRST. Run offline / during a write freeze.
   // Idempotent: only documents whose _id is still ObjectId are rewritten.
   db.MyCollection.find({ _id: { $type: "objectId" } }).forEach(d => {
     const oldId = d._id;
     const newId = String(oldId);
     // skip if a string-id document already exists (partial re-run safety)
     if (db.MyCollection.findOne({ _id: newId })) {
       print(`skip existing string id ${newId}`);
       return;
     }
     const copy = Object.assign({}, d, { _id: newId });
     db.MyCollection.insertOne(copy);
     db.MyCollection.deleteOne({ _id: oldId });
   });
   ```

   Apply the same ObjectId→string rewrite to every affected field:

   - entity `_id` for each collection that uses plain string IDs
   - join collections (`[Parent~Child(Prop)]` / `[(Prop)Parent~Child(Prop)]`) `ParentID` and `ChildID`
   - `One<T>.ID` reference fields (and `One<T,TIdentity>` when the identity is string)
   - `ModifiedBy.UserID`
   - nested/embedded documents that stored an `Entity`-style string ID as ObjectId

   Do **not** convert `[BINARY_CHUNKS].FileID` (see below). Nested arrays/subdocuments need the same `$type: "objectId"` filter on the nested path, not only top-level `_id`.

**Always convert (or accept string storage for) library-owned string reference fields:** `One<T>.ID` and `ModifiedBy.UserID` no longer use `[AsObjectId]` and always store/read plain strings. Leaving those as BSON ObjectId will fail deserialization after upgrade even if entity `_id`s are kept as ObjectId via the class map.

**File chunks:** `[BINARY_CHUNKS].FileID` still uses `[AsObjectId]`, so retain existing BSON ObjectId values for ObjectId-format file IDs. This remains true even if file entity metadata `_id` values are converted to BSON strings: the chunk serializer converts an ObjectId-format parent ID string to BSON ObjectId when querying, so existing chunks continue to match. Do **not** convert those `FileID` values to BSON strings; current library filters would not match them. Non-ObjectId-format file IDs are stored as strings automatically.

### NEW

- One-to-many and many-to-many relationships (add/remove, children count, children/parents queryables and fluents, cascade deletes) now support entities with `string`, custom-format `string`, `long`, `ObjectId`, `Guid` and custom-represented (e.g. `[BsonRepresentation(BsonType.ObjectId)] string`) ID properties. Join records and queries always use the stored representation of the ID, eliminating silent empty results for custom-represented IDs.
- Batch raw-ID APIs (`DeleteAsync`, `Many.AddAsync`/`RemoveAsync`, `ParentsQueryable`/`ParentsFluent`) accept value-type ID sequences via generic `IReadOnlyList<TId> where TId : struct` overloads (`Guid[]`, `long[]`, `ObjectId[]`, etc.).
- Expression/filter-based `DeleteAsync<T>` projects matched IDs as raw `BsonDocument` values, so Guid and custom-represented IDs cascade-delete correctly.
- Add `SetMigrationActivator(Func<Type, IMigration>)` method to support custom activation of migration classes.

### IMPROVEMENTS

- Upgrade mongodb driver to latest.

[//]: # (### FIXES)
