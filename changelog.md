v26 adds support for more relationship ID types, makes custom ID generation easier, and fixes issues with filtered deletes and batch saves.

## Before upgrading

**Existing databases need attention.** String IDs and references now use BSON strings by default instead of BSON ObjectIds. Before upgrading, choose a compatibility mapping or migrate your data. Keeping entity IDs as ObjectIds does not remove the need to migrate affected reference fields.

Read the [v26 upgrade guide](https://github.com/dj-nitehawk/MongoDB.Entities/blob/master/Documentation/wiki/Upgrade-to-v26.md) for the required steps, code examples, and file-storage exception.

## Breaking changes

- **String ID storage has changed.** `Entity.ID`, `One<T>.ID`, and `ModifiedBy.UserID` no longer have `[AsObjectId]` applied automatically. Follow the upgrade guide before reading existing data.
- **Custom ID generation uses the driver's `IIdGenerator`.** If you overrode `GenerateNewID()`, register a generator with `DB.RegisterIdGenerator<TEntity>()` instead. Remove `HasDefaultID()` overrides and use the extension method.
- **Relationship APIs support more ID types.** `[AsBsonId]` has been removed, and join-record types and parent-query signatures have changed. Review the guide if you use these APIs directly.
- **Guid IDs need an explicit serializer.** Register it before initializing the database.
- **Serializer registration happens earlier.** Register custom `Date`, `FuzzyString`, or `decimal` serializers before the first lookup of those types, rather than relying on database initialization timing.

## New features

- One-to-many and many-to-many relationships now support numeric, Guid, ObjectId, and custom IDs, including custom BSON representations.
- Configure ID generation per entity with `DB.RegisterIdGenerator<TEntity>()`, including for classes that inherit `Entity`.
- Pass value-type ID arrays, such as `Guid[]` or `long[]`, directly to batch delete and relationship APIs.

## Fixes

- Deleting an entity excluded by a global filter no longer removes its relationships or file chunks.
- Filter-based deletes now correctly handle Guid and custom-represented IDs when cleaning up relationships.
- Full and partial batch saves now support sequences that can only be enumerated once.

## Dependencies

- Updated MongoDB.Driver to 3.12.0 and SharpCompress to 0.50.5.