### BREAKING CHANGES

- **Relationships now work with any entity ID type/representation (dynamic IDs).** `[AsBsonId]` has been removed and join records (`JoinRecord.ParentID`/`ChildID`) are now `BsonValue` properties that hold the exact stored representation of the related entities' `_id` values (string, ObjectId, long, Guid, custom-represented IDs, etc.). `JoinRecord` now derives from `ObjectIdEntity` instead of `Entity`.
- **`[AsObjectId]` is no longer applied by the library.** The attribute still exists for opt-in use on your own properties, but it has been removed from `Entity.ID`, `One<T>.ID`, `ModifiedBy.UserID` and the internal file chunk reference. String IDs are now stored in MongoDB as plain strings unless you decorate them yourself (e.g. with `[AsObjectId]` or `[BsonRepresentation(BsonType.ObjectId)]`).
- `Many<TChild,TParent>.ParentsQueryable(...)` now accepts `object`/`IEnumerable<object>` child IDs instead of `string`/`IEnumerable<string>`.
- **Guid IDs:** entities with `Guid` ID properties require a Guid serializer to be registered before `DB.InitAsync`, e.g. `BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));`
- **`HasDefaultID()` instance methods removed** from `Entity`, `ObjectIdEntity` and internal types. Unsaved-entity detection compares the ID property against the default value of its type via the (now public) `HasDefaultID()` extension method. Remove your own implementations/overrides; note that overrides were already ignored by the library internally, so behavior does not change.
- **`GenerateNewID()` removed from `IEntity` and all entity base classes** — `IEntity` is now a marker interface. ID generation is driven by the mongodb driver's `IIdGenerator`, resolved per entity type in this order: a generator registered with the new `DB.RegisterIdGenerator<TEntity>(...)` (works for any entity, including `Entity` subclasses), the generator on the entity's `BsonClassMap`, one registered for the ID's CLR type via `BsonSerializer.RegisterIdGenerator(...)`, then library defaults for `string` (ObjectId-formatted strings, same as before), `ObjectId` and `Guid` IDs. A `GenerateNewID()` extension method replaces the instance method. **If you overrode `GenerateNewID()` for a custom ID format, register an `IIdGenerator` via `DB.RegisterIdGenerator<TEntity>()` instead** — otherwise string IDs will silently receive ObjectId-format values, and IDs of other types without a resolvable generator will throw on save.
- The library's global serializers and conventions now register via a module initializer (previously the `DB` static constructor), so user registrations of class maps, serializers and id generators made before `DB.InitAsync()` are always applied on top of — never instead of — the library's own. The library's `Date`/`FuzzyString`/`decimal` serializers now use `TryRegisterSerializer`, so a user-registered serializer for those types wins instead of causing an exception.

#### Data migration for existing databases

Databases written by previous versions store ObjectId-parseable string IDs as **BSON ObjectId** in entity `_id` fields, join record `ParentID`/`ChildID` values and `One<T>.ID`/`ModifiedBy.UserID` references. After this upgrade, plain string ID properties can no longer read those values and new writes would store plain strings. Choose one of:

1. **Keep the old storage format (recommended):** decorate your entities' ID properties with `[AsObjectId]` (or `[BsonRepresentation(BsonType.ObjectId)]`). Entity `_id`s and join records keep matching, since join records now always follow the entity's own stored representation. Note that `One<T>.ID` and `ModifiedBy.UserID` are library properties and are now always stored as plain strings, so existing ObjectId-valued reference documents must still be converted per option 2.
2. **Convert stored ObjectIds to strings**, e.g. in `mongosh` for each affected collection/field:
   ```js
   db.MyCollection.find({ _id: { $type: "objectId" } }).forEach(d => {
     db.MyCollection.insertOne({ ...d, _id: String(d._id) });
     db.MyCollection.deleteOne({ _id: d._id });
   });
   // and for join collections ([Parent~Child(Prop)]) / One<T> reference fields,
   // $convert the ParentID/ChildID/ID values from objectId to string accordingly.
   ```

### NEW

- One-to-many and many-to-many relationships (add/remove, children count, children/parents queryables and fluents, cascade deletes) now support entities with `string`, custom-format `string`, `long`, `ObjectId`, `Guid` and custom-represented (e.g. `[BsonRepresentation(BsonType.ObjectId)] string`) ID properties. Join records and queries always use the stored representation of the ID, eliminating silent empty results for custom-represented IDs.
- Add `SetMigrationActivator(Func<Type, IMigration>)` method to support custom activation of migration classes.

### IMPROVEMENTS

- Upgrade mongodb driver to latest.

[//]: # (### FIXES)
