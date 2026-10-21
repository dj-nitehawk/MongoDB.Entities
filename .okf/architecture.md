---
type: Architecture
title: Architecture
description: DB facade boundaries, view and session ownership, and persistence contracts.
tags: [architecture]
---

# Architecture

## Boundaries and runtime

Async data-access library over `MongoDB.Driver`, not an application host. `DB` partials create fluent builders over driver collections and sessions. `Core/` owns entity contracts, mapping/cache, serializers, transactions, watchers, templates, and file storage; `Relationships/` owns references and joins.

- Library runtime dependencies are the driver and an explicit `SharpCompress` mitigation pin. Tests depend on the library; Benchmark depends on both. No reverse dependencies or heavy application frameworks.
- `InitAsync` caches clients by `MongoClientSettings` and DB instances by client/database. The first database selected for default client settings supplies `DB.Default`; initialization normally pings unless `skipNetworkPing` is set.
- Auth belongs to caller-supplied settings/connection strings. Optional `IServiceProvider` and migration activator support construction, not a framework dependency.
- Persisted types implement marker `IEntity`, usually via `Entity`. Collection names come from the type or `[Collection]`.
- Migration names supply numeric ordering; history lives in `_migration_history_`.

## Views, filters, and sessions

- `DB(source)` snapshots filter registrations and prepend flags into an independent dictionary. Existing builders still observe registrations on their own retained dictionary; builders created before the first registration keep their previous behavior.
- `WithModifiedBy()` returns an audit view preserving database, filters, and session, without mutating the source audit data. A view shares but never owns a transaction session. Only the originating `Transaction` commits, aborts, and disposes it; a new `Transaction` always starts its own session, even from a transaction view.
- Audit views and direct-derived transactions retain overridden save/update hooks via independent, private shallow-cloned hook contexts. Dispatch synchronizes audit data, session, ignore state, and the owning view's filter dictionary. Reference-type dependencies remain shared; contexts are not exposed as disposable/session owners.
- Ordinary `DB(source)` copies do not capture source overrides unless a hook context already exists; filter copying does not alter ordinary wrapper session behavior.

## Relationships and cascading deletes

- `One<>` stores referenced IDs; `Many<>` uses join collections. Join IDs must use the entity ID's **stored BSON representation**, via `GetBsonId()` / `Cache<T>.IdToBsonValue`; queryable joins use `BsonValueIdExpression`. Raw CLR values can silently miss custom-represented IDs.
- `Cache<T>.ReferenceCollections` stores collection names and monotonically accumulated `JoinRecordRole` flags, never database-bound handles. Register both endpoints, swap inverse roles, and atomically OR missing flags. Cleanup resolves names against the current DB and matches only the endpoint's registered role; only self-references match both fields.
- Filter/predicate deletes and direct-ID global-filter eligibility project raw `BsonDocument` IDs. Do not project via `object`/`ObjectSerializer`, which breaks Guid Standard IDs.
- Eligibility reads are snapshots, not permission to clean up joins. Filtered non-file deletes with registered relationships sequentially `FindOneAndDeleteAsync` each distinct candidate with the current filter; only returned stored IDs cascade. Zero returned IDs means no side writes.
- For filtered non-file deletes without relationships, an operation-time empty-registry check selects a session-aware bulk delete retaining the global filter. Register relationships before concurrent deletion: late registration cannot add cleanup to an in-flight bulk delete and can leave orphaned joins. Later operations see registrations. Unfiltered/ignored-filter deletes retain bulk behavior.
- File deletes follow the reference-conditional path below. Without a caller transaction, failures can leave earlier deletions and orphaned joins; retries need not repair them. Do not promise cross-collection atomicity. Callers own transaction conflict handling.
- Cascade deletes reject non-default cancellation tokens without a session, to avoid cancellation between collection writes.

## File storage

`FileEntity<T>` metadata references `[BINARY_CHUNKS]`, not GridFS. `FileMetadata` owns managed-field mapping/save policy; `FileStorage` owns storage I/O and legacy preflight; `DB.DeleteCascadingAsync` owns ID conversion/validation, filter eligibility, and relationship cleanup.

- New uploads use independent `BinaryStorageID` keys; new chunks omit legacy `FileID`. Legacy chunk `FileID` retains `[AsObjectId]`, and legacy queries exclude keyed chunks. Stream operations reload persisted reference/upload status, projecting only those fields through inherited BSON mappings, with the caller's session/token. They do not materialize application metadata. `Data(db)` binds a fresh streamer to that DB; pass transaction sessions explicitly.
- Replacement stages a fresh key, validates checksum, and conditionally switches reference/statistics atomically. Cleanup targets only the replaced new-format key; legacy chunks remain. Explicit chunk clearing conditionally publishes a tombstone key and empty statistics.
- Cascades preflight all legacy candidates before writes, conditionally delete metadata with its filter and observed storage reference, then delete only referenced chunks. `DB` waits for successfully deleted stored IDs before relationship cleanup, preserving joins for skipped files, including mixed-result batches.
- Legacy destructive operations take one operation-local collection inventory per batch (none for empty batches), then session-bound per-ID conflict reads. Conflicts or inventory errors fail before cascade writes. Database-wide listing/read permissions are required. This is not proof of ownership against concurrent legacy writes or custom schemas; reconcile ambiguity offline, never guess.
- Resolve inherited managed-field BSON names through concrete `AllMemberMaps`, not declared-only `GetMemberMap`. Entity field renames must not alter the separate chunk schema.
- Full saves atomically replace application metadata while retaining current server upload fields, defaulting missing fields to empty. `FileMetadata.SaveUpdate` uses `$replaceWith`/`$mergeObjects`, wrapping serialized metadata in `$literal`; omitted mapped fields and removed extra elements disappear. Partial/preserving saves retain omission semantics and exclude managed fields. Raw updates and `Replace` bypass these protections.
- Generated-ID full saves and all explicit inserts reset upload state, so a new identity never copies binary ownership. Existing identities preserve persisted state; partial saves do not reset in-memory state.
- Index initialization is shared per client/chunk namespace and awaited before upload. Evict only the exact failed cache entry, permitting retry without removing a newer attempt.
- Upgrade every application instance before new-format uploads; old libraries cannot resolve new keys. No bulk migration is required. Interrupted/uncertain uploads may leave orphans; existing shared-key data needs offline repair with writes frozen and backups, never blind chunk deletion. See [v26 upgrade guidance](../Documentation/wiki/Upgrade-to-v26.md).

## Mapping and write preparation

- `Cache<T>` derives partial-update eligibility from `BsonClassMap.AllMemberMaps`, excluding the ID and relationships. Unmapped/ignored properties stay excluded even when explicitly selected, across partial/preserving saves and both builders' entity modifications. This prevents new writes, not removal of old stored fields. Register mappings before first use.
- Batch full saves, inserts, and single/batch partial saves share `DB.PrepAfterId`: ID/timestamps, audit, then save hook. `PrepForSave` adds file reset for generated-ID full saves/all inserts. Single full saves intentionally call the hook before ID preparation; empty IDs in hooks are not an insert detector.
- Pipeline builders invoke `OnBeforeUpdate` once before writing. Throwing hooks propagate; hook-added regular modifications fail before writes. Compare definition counts around the hook, not against zero, because audit definitions can pre-exist. Do not discard hook definitions or translate update operators to pipeline expressions; modifying hooks require regular updates.
- Global conventions/serializers register in `Core/LibraryInitializer.cs`, not a DB static constructor. Lazy provider defaults for `Date`, `FuzzyString`, and decimal allow caller serializer registration before first lookup. See [identity/serializer timing](gotchas.md#identity-and-serialization).

## Sources

- `MongoDB.Entities/DB/` (`DB.cs`, `DB.Transaction.cs`, `DB.Delete.cs`, `DB.Save.cs`)
- `MongoDB.Entities/Core/` (cache, file metadata/storage/streaming, module initialization)
- `MongoDB.Entities/Relationships/`
- `MongoDB.Entities/Builders/` (`Update.cs`, `UpdateAndGet.cs`)
- `Documentation/wiki/Upgrade-to-v26.md`
