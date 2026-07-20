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
- Relationships: referenced IDs (`One<>`) or join collections (`Many<>` / `JoinRecord`). Cascade metadata caches join-collection names, not database-bound handles; deletes resolve each name against the current `DB` so multi-database cleanup remains isolated.
- Join records store `ParentID`/`ChildID` as `BsonValue` holding the *stored representation* of the entity's `_id` (via `GetBsonId()` / `Cache<T>.IdToBsonValue`); queryable joins/lookups key entities with `Cache<T>.BsonValueIdExpression`. Never write raw CLR ID values into join records; custom-represented IDs would silently match nothing.
- Filter/expression `DeleteAsync<T>` loads matched IDs via `Find<T, BsonDocument>` (raw BSON), then cascade-deletes with `idsAreStoredValues: true`. Never project IDs through `object`/`ObjectSerializer` (breaks Guid Standard representation).
- Direct-ID cascade deletes (`DeleteCascadingAsync`) also respect global filters: when a filter is active for `T` and not ignored, eligible IDs are resolved first via the same raw-BSON projection path; only that set is used for entity, join-record, and file-chunk deletes. Empty eligibility → acknowledged zero, no side-collection writes.
- File storage: `FileEntity<T>` metadata + `[BINARY_CHUNKS]` chunk docs (not GridFS API).
- Global serializers/conventions register in module init (`Core/LibraryInitializer.cs`, not a `DB` static ctor): ignore extra elements; ignore many-props; custom `Date` / `FuzzyString` / decimal via `IBsonSerializationProvider` (lazy; user `RegisterSerializer` before first lookup wins).

## Security / auth (library surface)
- Auth is caller-supplied via `MongoClientSettings` / connection string. Library does not manage credentials.
- CI compose uses root user + keyfile replica set (test-only; see `operations.md`).

## Invariants
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
