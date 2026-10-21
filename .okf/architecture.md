---
type: Architecture
title: Architecture
description: Partial DB facade over MongoDB.Driver with entity contracts, builders, and relationship helpers.
tags: [architecture]
---

# Architecture

## Style
Library-style data-access layer: thin facade + fluent builders over official driver types (`IMongoDatabase`, `IMongoCollection`, filter/update builders). No application host. Async-only public data APIs.

## Components
| Component | Role |
| --- | --- |
| `DB` (partial) | Connection cache, default instance, ops entrypoints (`Save`, `Find`, …) |
| `Core/` | Entities, attributes, serializers, cache, transactions, watchers, templates |
| `Builders/` | Fluent query/update/index/paged-search builders returned by `DB` methods |
| `Relationships/` | `One<>`, `Many<>`, `JoinRecord` |
| `Extensions/` | Entity/collection/relationship helpers (e.g. `InitOneToMany`) |
| `Migrations/` | `IMigration` + history entity `Migration` → collection `_migration_history_` |
| `Tests/` | MSTest integration suite against real MongoDB |
| `Benchmark/` | BenchmarkDotNet microbenchmarks |
| `Documentation/` | DocFX wiki + API docs |

## Dependency rules
- Library depends only on `MongoDB.Driver` (+ transitive; `SharpCompress` pinned for vulnerability workaround).
- `Tests` → library; `Benchmark` → library + `Tests` (reuses test models/helpers).
- Do not add reverse deps from library to Tests/Benchmark/Documentation.
- Prefer extending `DB` via new partial files under `MongoDB.Entities/DB/` and builders under `Builders/` rather than unrelated namespaces.

## Communication / runtime model
```
App → DB.InitAsync(dbName, settings?) → cached MongoClient + DB instance
     → DB.Save/Find/Update/… → IMongoCollection / sessions
     → Transaction() → client session + multi-doc txn
```
- Clients keyed by `MongoClientSettings`; DB instances keyed by client + database name (`ConcurrentDictionary` caches).
- First successful init for default client settings becomes `_defaultInstance` (`DB.Default`).
- Optional ASP.NET DI: `SetServiceProvider` / `SetMigrationActivator` for migration construction.

## Persistence
- One MongoDB collection per entity type (name from type or `[Collection]`).
- Relationships: referenced IDs (`One<>`) or join collections (`Many<>` / `JoinRecord`). Cascade metadata caches join-collection names and each entity type's parent/child roles, not database-bound handles; deletes resolve each name against the current `DB` so multi-database cleanup remains isolated. Initializers register both endpoint types, including one-to-many children without an inverse property. Repeated registrations atomically merge roles; only self-references match both ID fields during cascade cleanup.
- Join records store `ParentID`/`ChildID` as `BsonValue` holding the *stored representation* of the entity's `_id` (via `GetBsonId()` / `Cache<T>.IdToBsonValue`); queryable joins/lookups key entities with `Cache<T>.BsonValueIdExpression`. Never write raw CLR ID values into join records; custom-represented IDs would silently match nothing.
- Filter/expression `DeleteAsync<T>` loads matched IDs via `Find<T, BsonDocument>` (raw BSON), then cascade-deletes with `idsAreStoredValues: true`. Never project IDs through `object`/`ObjectSerializer` (breaks Guid Standard representation).
- Direct-ID cascade deletes (`DeleteCascadingAsync`) also respect global filters: when a filter is active for `T` and not ignored, eligible IDs are resolved first via the same raw-BSON projection path. For non-file entities without registered relationships, an operation-time empty-registry check selects a direct session-aware bulk delete with the merged global filter and no relationship cleanup. Relationship registration must precede concurrent deletes: registration after bulk selection is not retroactive and can leave joins for deleted entities orphaned, but cannot trigger stale-candidate cleanup of surviving entities. Later deletes see registrations. For non-file entities with registered relationships, that snapshot is not sufficient to authorize relationship cleanup: each distinct candidate is deleted with `FindOneAndDeleteAsync`, rechecking the global filter and projecting its stored BSON ID. Only returned IDs cascade to role-aware joins; zero returned IDs means acknowledged zero without side writes. Conditional deletes are sequential and use the caller's session/cancellation policy. This adds one delete round trip per candidate; unfiltered/ignored-filter deletes retain the bulk path. Failures propagate, but without a caller transaction earlier deletes are not rolled back and orphaned joins can remain, including when a later candidate delete fails. A retry need not repair those orphans. Caller transactions retain ownership and provide cross-collection rollback; conflicts must be handled by the caller.
- File storage: `FileEntity<T>` metadata + `[BINARY_CHUNKS]` chunk docs (not GridFS API). Each completed new upload has an independent `BinaryStorageID` in metadata/chunks; new chunks omit `FileID`. Stream operations reload persisted metadata, including direct-ID access. Replacements stage a fresh key, verify the checksum, then conditionally switch the old reference and statistics atomically. Cleanup deletes only the replaced new key; legacy chunks are retained on replacement. Explicit chunk deletion conditionally publishes a tombstone key and cleared statistics. Cascades retrieve eligible metadata references, conditionally delete each metadata document, then delete only its referenced chunks. FileStorage returns the successfully deleted stored BSON IDs; DB waits for those results before cascading role-aware relationship cleanup, preserving relationships for files skipped after concurrent reference changes, including mixed-result batches. Filtered non-file cascade ordering follows the conditional entity-first path described above; unfiltered ordering is unchanged. Legacy destructive operations inspect other actual collections for conflicting file metadata and fail before cascade writes on established conflicts or inventory errors. This does not prove ownership against concurrent legacy writes or custom legacy schemas.
- Streaming metadata reads project only `BinaryStorageID` and `UploadSuccessful` into a private `DataStreamer<T>` snapshot, with inherited BSON member mappings resolved by the driver's expression projection. Upload, download, and explicit chunk clearing still reload persisted state with the caller's session and cancellation token, without materializing application metadata.
- `IFileEntity` marks file entities internally and supports resetting upload state for inserts. `Cache<T>.UpdatableProps` excludes managed file metadata, protecting partial/preserving saves; full single/batch file saves atomically preserve persisted upload fields with empty defaults for missing values, so stale/projected entities do not reset existing storage references or statistics. Generated-ID full saves and all explicit insert paths reset upload state before insertion. Copying a loaded file to a new identity never copies binary ownership. Raw updates and `Replace` bypass these protections.
- Legacy ownership preflight owns one operation-local collection inventory for all legacy IDs in a cascade batch, retaining per-ID conflict queries and session-bound reads. Explicit chunk clearing uses the same preflight with one ID. Empty legacy batches skip inventory entirely. Inventory is neither cached across operations nor an ownership snapshot; the whole preflight must finish before any cascade writes.
- `FileMetadata` owns the pure managed-property policy, inherited BSON-member name resolution, and full-save update construction. `Cache<T>` and full-save entrypoints depend on this helper directly; `FileStorage` uses its mapped-name lookup for conditional deletion and retains storage I/O orchestration. Full file saves use an update pipeline with `$replaceWith`/`$mergeObjects`: serialized application metadata is wrapped in `$literal`, while ID and managed upload fields come from the current server document. Missing upload fields use empty defaults. Replacement removes omitted mapped fields and deleted extra-element entries without reading metadata into the application or racing upload reference changes. Partial/preserving saves retain their existing omission behavior.
- Batch full saves, explicit inserts, and single/batch partial saves share `DB.PrepAfterId`: ID/timestamps, audit assignment, then save hook. `DB.PrepForSave` wraps that sequence with a file upload reset for generated-ID full saves or all explicit inserts. Partial saves do not reset in-memory upload state. Single full saves retain their separate pre-ID hook ordering. Write models, enumeration, and session dispatch stay at the call sites.
- Global serializers/conventions register in module init (`Core/LibraryInitializer.cs`, not a `DB` static ctor): ignore extra elements; ignore many-props; custom `Date` / `FuzzyString` / decimal via `IBsonSerializationProvider` (lazy; user `RegisterSerializer` before first lookup wins).

## Security / auth (library surface)
- Auth is caller-supplied via `MongoClientSettings` / connection string. Library does not manage credentials.
- CI compose uses root user + keyfile replica set (test-only; see `operations.md`).

## Invariants
- `WithModifiedBy()` creates an audit view that preserves the source session, database, and global filters without changing the source audit data. Audit views share, but do not own, a transaction session; only the originating `Transaction` commits, aborts, and disposes it. Creating a new `Transaction` always starts its own session, including from an audit view.
- Audit views and transactions created directly from a derived `DB` retain derived save/update hooks through a private shallow-cloned hook context. Base hook dispatch synchronizes that context's audit data, session, and filter-ignore flag before resolving hooks. Copies retain independent hook contexts, including chained views and transactions created from an audited view. Reference-type dependencies remain shared; the hook context is never exposed as another disposable/session owner. Ordinary `DB(source)` construction still does not capture source overrides unless an audit hook context already exists.
- Types persisted through library APIs implement `IEntity` (typically inherit `Entity`).
- New entities without ID (`HasDefaultID()`: generator `IsEmpty(id)` when a generator is resolved, else compare to `Cache<T>.IdDefaultValue`) get one from `Cache<T>.IdGenerator` via the `GenerateNewID()` extension. `IEntity` is a marker interface. Generator resolution: `DB.RegisterIdGenerator<T>()` (overrides, any time via `Cache.SetIdGenerator`) → class map `IdGenerator` → `BsonSerializer.LookupIdGenerator(idType)` → library defaults (string→ObjectId-string, ObjectId, Guid) → null. Missing generator is allowed on `Cache<T>` init so query/delete/manual-ID use works; generation fails only in `GenerateNewID()` when save actually needs a new ID.
- `Many<>` must be initialized on parent (`InitOneToMany` / `InitManyToMany`) before use.
- Migrations ordered by numeric prefix in type name; history in `_migration_history_`.
- `ChangeDefaultDatabase` is concurrency-sensitive; cancel watchers first (documented warning on API).
- Public API is async; keep new surface async.

## Sources
- `MongoDB.Entities/DB/DB.cs`
- `MongoDB.Entities/Core/LibraryInitializer.cs`
- `MongoDB.Entities/Core/Cache.cs`
- `MongoDB.Entities/Core/Entities/Entity.cs`
- `MongoDB.Entities/Relationships/Many.cs`
- `MongoDB.Entities/DB/DB.Migrate.cs`
