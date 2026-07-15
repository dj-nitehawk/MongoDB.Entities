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
- Relationships: referenced IDs (`One<>`) or join collections (`Many<>` / `JoinRecord`).
- Join records store `ParentID`/`ChildID` as `BsonValue` holding the *stored representation* of the entity's `_id` (via `GetBsonId()` / `Cache<T>.IdToBsonValue`); queryable joins/lookups key entities with `Cache<T>.BsonValueIdExpression`. Never write raw CLR ID values into join records — custom-represented IDs would silently match nothing.
- File storage: `FileEntity<T>` metadata + `[BINARY_CHUNKS]` chunk docs (not GridFS API).
- Conventions registered in `DB` static ctor: ignore extra elements; ignore many-props; custom `Date` / `FuzzyString` / decimal serializers.

## Security / auth (library surface)
- Auth is caller-supplied via `MongoClientSettings` / connection string. Library does not manage credentials.
- CI compose uses root user + keyfile replica set (test-only; see `operations.md`).

## Invariants
- Types persisted through library APIs implement `IEntity` (typically inherit `Entity`).
- New entities without ID: `GenerateNewID()` when the ID equals its type's default value (`HasDefaultID()` extension comparing against `Cache<T>.IdDefaultValue`; `IEntity` itself only declares `GenerateNewID`).
- `Many<>` must be initialized on parent (`InitOneToMany` / `InitManyToMany`) before use.
- Migrations ordered by numeric prefix in type name; history in `_migration_history_`.
- `ChangeDefaultDatabase` is concurrency-sensitive; cancel watchers first (documented warning on API).
- Public API is async; keep new surface async.

## Sources
- `MongoDB.Entities/DB/DB.cs`
- `MongoDB.Entities/Core/Entities/Entity.cs`
- `MongoDB.Entities/Relationships/Many.cs`
- `MongoDB.Entities/DB/DB.Migrate.cs`
