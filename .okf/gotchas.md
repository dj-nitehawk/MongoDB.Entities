---
type: Reference
title: Gotchas
description: Non-obvious traps for agents working on MongoDB.Entities.
tags: [gotcha]
---

# Gotchas

- **Tests need Mongo.** Default path expects compose on `localhost:27017` with `admin`/`password`, `rs0`, `authSource=admin`. Without it, use `MONGODB_ENTITIES_TESTCONTAINERS=1` (Docker required).
- **Replica set required** for multi-doc transactions and related tests; standalone Mongo will fail those cases.
- **Replica set member host must be host-reachable.** Tests connect with `localhost:27017` + `replicaSet=rs0`. After hello/isMaster the driver follows the advertised member host. If `rs.initiate` used the compose service name (`mongodb:27017`), host-side clients get `Name or service not known` / server selection timeout on `Unspecified/mongodb:27017`. Use `localhost:27017` (as in `docker-compose.ci.yml`) or reconfig: `cfg.members[0].host='localhost:27017'; rs.reconfig(cfg,{force:true})`.
- **`Tests/.mongo-keyfile`:** compose mounts it; CI creates with `chmod 600` and `chown 999:999`. Wrong perms → container auth/keyfile errors.
- **`[assembly: DoNotParallelize]`**: do not enable parallel MSTest without redesigning shared DB usage.
- **`Many<>` uninitialized** → runtime failures; must `InitOneToMany` / `InitManyToMany` in entity ctor (see relationship wiki/tests).
- **`[BINARY_CHUNKS].FileID` keeps `[AsObjectId]`** so ObjectId-format parent IDs still match legacy chunk docs after `Entity.ID` stopped using `[AsObjectId]`. File entity `_id` storage still follows the entity mapping (plain string by default).
- **Cannot decorate or remap inherited `Entity.ID` per concrete type:** `BsonClassMap.RegisterClassMap<Book>(...).MapIdProperty(b => b.ID)` throws because `ID` is declared on `Entity`. To keep legacy BSON ObjectId `_id`s, map the `Entity` base member before first use (affects every `Entity` subclass), including `StringSerializer(BsonType.ObjectId)` and `StringObjectIdGenerator`; for per-entity storage, implement `IEntity` and own the `[AsObjectId]`/`[ObjectId]` ID property.
- **`JoinRecord` `_id` is server-generated** (upsert writes never set it), so it's a BSON ObjectId, hence `JoinRecord : ObjectIdEntity`. Don't rebase it on `Entity` (plain string ID can't deserialize ObjectId `_id`).
- **`Guid` entity IDs** need `BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard))` before init (tests do this in `Tests/Init.cs`); the library does not register one. Expression/filter-based `DeleteAsync<T>` projects IDs as `BsonDocument` (not `object`) so Guid/custom-represented IDs cascade-delete correctly.
- **Direct-ID deletes + global filters:** join-record and file-chunk cascades use only IDs that survive the entity global filter (eligibility query before side deletes). Without this, a filtered-out `FileEntity` could keep metadata while losing binary chunks.
- **Value-type batch IDs:** use the typed overloads `DeleteAsync<T,TId>(IReadOnlyList<TId>)`, `Many.AddAsync<TId>/RemoveAsync<TId>`, `ParentsQueryable/ParentsFluent<TId>` with `where TId : struct` (arrays like `Guid[]`/`long[]`/`ObjectId[]`). Do **not** use `IEnumerable<TId>` for those overloads; `string` implements `IEnumerable<char>` and would steal the generic method. Existing `IEnumerable<object?>` overloads remain for reference/boxed IDs.
- **Custom ID generation** = registering an `IIdGenerator` (per entity via `DB.RegisterIdGenerator<T>()`, which works for `Entity` subclasses too; or per ID type via `BsonSerializer.RegisterIdGenerator`). There is no `GenerateNewID()` to override anymore. Without a resolvable generator, string IDs silently get ObjectId-format values. Other ID types can still be cached/queried/deleted and accept manually assigned IDs; missing generators throw only when `GenerateNewID()` runs (typically save of an entity with an empty ID).
- **`HasDefaultID()` uses `IIdGenerator.IsEmpty`** when a generator is resolved (so `string.Empty` and custom empty sentinels regenerate). With no generator it falls back to comparing against the ID member's default value.
- **Generator registration timing differs by level:** `DB.RegisterIdGenerator<T>()` is order-independent (`Cache.SetIdGenerator` overrides even after first use). `BsonSerializer.RegisterIdGenerator` / class-map `SetIdGenerator` are only read during `Cache<T>` static init; they must run **before first use** of that entity type (including first save/query that touches `Cache<T>`).
- **Global conventions register via module initializer** (`Core/LibraryInitializer.cs`, not a `DB` static ctor). `Date`/`FuzzyString`/`decimal` defaults come from an `IBsonSerializationProvider`, so users can `BsonSerializer.RegisterSerializer` for those types before first lookup and win; do not rely on `TryRegisterSerializer` to override an existing different serializer (it throws). The `ModuleInitializerAttribute` shim exists because netstandard2.1 lacks it.
- **`DB.Default` / `Instance`** throw if `InitAsync` never ran for that client/name.
- **`ChangeDefaultDatabase`** is explicitly unsafe under concurrency; cancel watchers first.
- **Migration discovery** excludes many assembly name prefixes; test assembly name `MongoDB.Entities.Tests` is special-cased. Prefer `MigrateAsync<T>()` or `MigrationsAsync` when discovery is ambiguous.
- **Migration class names** must split on `_` with numeric order prefix or execution throws.
- **Library LangVersion 13** (not latest) due to DocFX; don’t bump casually without checking docs build.
- **`SharpCompress` package** is a deliberate direct reference for vuln mitigation; remove only when driver transitive is fixed.
- **Solution format is `.slnx`**, not classic `.sln`.
- **Line endings CRLF** per `.editorconfig`; preserve when editing.
- **Public docs site ≠ repo README depth**: behavioral detail lives in `Documentation/wiki/` and XML docs; verify against source.
- **Do not commit secrets.** CI compose passwords are fixtures; NuGet key is Actions secret only.

## Sources
- `Tests/Init.cs`
- `docker-compose.ci.yml`
- `MongoDB.Entities/DB/DB.cs`
- `MongoDB.Entities/DB/DB.Migrate.cs`
- `MongoDB.Entities/MongoDB.Entities.csproj`
