---
type: Reference
title: Gotchas
description: Identity compatibility traps, environment failures, and task-specific safety pointers.
tags: [gotcha]
---

# Gotchas

## Identity and serialization

- **Inherited `Entity.ID` is a plain BSON string.** A concrete class cannot re-attribute it or `MapIdProperty` an inherited declaration. Legacy BSON ObjectId storage requires mapping the `Entity` base member before first use (affects all subclasses), or implementing `IEntity` with an owned ID property. `StringSerializer(BsonType.ObjectId)` + `StringObjectIdGenerator` supports only ObjectId-formatted strings; use owned `[AsObjectId]` mappings for mixed/per-entity storage. See [v26 compatibility](../Documentation/wiki/Upgrade-to-v26.md).
- **Guid IDs need caller registration** of `new GuidSerializer(GuidRepresentation.Standard)` before init; the library does not register it. Preserve stored BSON IDs through joins/deletes, as described in [cascade contracts](architecture.md#relationships-and-cascading-deletes).
- **Value-type batch ID overloads use `IReadOnlyList<TId> where TId : struct`**, for `DeleteAsync<T,TId>`, `Many.AddAsync/RemoveAsync`, and `ParentsQueryable/ParentsFluent`. Do not substitute `IEnumerable<TId>`: strings implement `IEnumerable<char>` and steal generic overload resolution. Reference/boxed IDs retain `IEnumerable<object?>` overloads.
- **ID generation uses `IIdGenerator`, not an overridable entity method.** Resolution is entity-level `DB.RegisterIdGenerator<T>()`, then class-map generator, driver type generator, library defaults (string/ObjectId/Guid), then none. String fallback silently generates ObjectId-formatted values. Other types allow manual IDs/query/delete without a generator; generation fails only when needed.
- **Registration timing differs:** entity-level `DB.RegisterIdGenerator<T>()` can override after cache initialization; type-level/class-map generators must be registered before first `Cache<T>` use. `HasDefaultID()` uses the resolved generator's `IsEmpty`, else the mapped default value, so empty strings/custom sentinels may regenerate.
- **`JoinRecord : ObjectIdEntity` is intentional:** upserts leave `_id` server-generated as BSON ObjectId. Rebasing on plain-string `Entity` breaks reads.
- **Serializer defaults are lazy module-initializer providers.** Register custom `Date`/`FuzzyString`/decimal serializers before first lookup. `TryRegisterSerializer` does not replace a different existing serializer (it throws). Keep the `ModuleInitializerAttribute` shim for netstandard2.1.
- **Wiki examples:** no implicit `DateTime` → `Date` or string → `FuzzyString`. Use constructors or `.ToDate()` / `.ToFuzzy()`; fuzzy relevance sorts target `.Value`.

## Behavioral traps

- **Relationships need initialization/binding.** `InitOneToMany` / `InitManyToMany` default to `DB.Default`; for tenants, pass the intended DB and rebind after deserialization. Models can deliberately defer initialization until explicit binding. See `Documentation/wiki/Multiple-Databases.md`.
- **Changing shared behavior?** Read [Architecture](architecture.md) for independent filter dictionaries, audit/transaction ownership, derived hook contexts, save-hook timing, mapped partial writes, pipeline-hook restrictions, and cascade race guarantees. Do not rebuild those contracts from a local call site alone.
- **File IDs resembling ObjectIds must be lowercase.** Upload/download/chunk clearing/cascades reject uppercase or mixed-case hex before chunk access/cascade writes. Validate the whole eligible batch first; never silently normalize distinct string metadata IDs. Non-hex custom IDs remain valid. This cannot repair earlier aliased/overwritten data.
- **File compatibility and ownership:** read [file storage](architecture.md#file-storage) before touching metadata/chunk schemas, saves, or deletion. Raw updates/`Replace` bypass upload-field protection; copying metadata is not copying bytes. Never infer safe legacy ownership or delete shared chunks without reconciliation.
- **Count filter bypass:** `CountAsync` honors its per-call `ignoreGlobalFilters`, not `DB.IgnoreGlobalFilters`. Estimated counts and raw collection access bypass entity filters.
- **Watchers** are keyed by DB instance/entity type; names are trimmed/lowercased. `MaxConnectionPoolSize` controls connections, not the .NET thread pool.
- **Initialization/default changes:** `DB.Default` / `Instance` throw before matching initialization. `ChangeDefaultDatabase` is unsafe under concurrency; cancel watchers first.
- **Migrations:** names need a numeric underscore prefix. Discovery excludes many assembly prefixes (test assembly is special-cased); prefer `MigrateAsync<T>()` or `MigrationsAsync` if discovery is ambiguous.

## Local Mongo and tooling

- **Tests need real MongoDB + replica set.** Use compose or Testcontainers as documented in [Testing](testing.md); standalone Mongo fails transaction cases. Do not enable parallel MSTest or rely on another test's audit setup.
- **Advertised replica-set host must be host-reachable.** Compose uses `localhost:27017`; an existing volume advertising `mongodb:27017` causes host clients to time out after discovery. For a local test replica set only, reconfigure its advertised member to `localhost:27017`.
- **Compose keyfile:** create `Tests/.mongo-keyfile` before startup, with mode `600` and container ownership `999:999`, as in `azure-pipelines.yml`. Wrong permissions cause auth/keyfile startup errors. Fixture credentials are test-only; never commit real credentials.
- **Toolchain constraints:** `.slnx`, library C# 13 (DocFX lag), and the explicit `SharpCompress` vulnerability mitigation pin are deliberate. See [Dependencies](dependencies.md) before changing them.
- **Generated docs:** never hand-edit `Documentation/api/` or `_site/`; source is wiki/XML docs. Match `.editorconfig` CRLF when editing. See [Workflows](workflows.md) for generation and snippet validation.

## Sources

- `MongoDB.Entities/Core/Cache.cs` and `MongoDB.Entities/Extensions/Identity.cs`
- `MongoDB.Entities/Core/LibraryInitializer.cs`
- `MongoDB.Entities/DB/` (`DB.Count.cs`, `DB.Migrate.cs`, `DB.cs`)
- `Tests/Init.cs` and `docker-compose.ci.yml`
- `Documentation/wiki/Upgrade-to-v26.md`
