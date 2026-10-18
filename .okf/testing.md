---
type: Playbook
title: Testing
description: MSTest integration tests against MongoDB 8.2 (compose or Testcontainers).
tags: [test]
---

# Testing

## Frameworks and layout
- **MSTest** (`MSTest.TestFramework` / adapter) + `Microsoft.NET.Test.Sdk`
- Project: `Tests/Tests.csproj` → `net10.0`, references library
- **Do not parallelize:** `[assembly: DoNotParallelize]` in `Tests/Init.cs`
- Assembly init: `InitTest.Init` registers Guid serializer, chooses Mongo mode, calls `InitTestDatabase("mongodb-entities-test")`
- Feature tests: `Tests/EntityTests/Test*.cs` (notable: `TestDeleting` global-filter cascade/join, `TestDynamicIdRelationships`, `TestEntityClassMap` filter-delete/Guid/value-ID lists, `TestIdGeneratorResolution`, `TestSerializerRegistration`, `TestMultiDb`, `TestDefaultDbChange`, `TestRelationships`, `TestTransactions`, `TestWatcher`)
- Models: `Tests/Models/**` (incl. `DynamicIdEntities.cs`, `NoGeneratorIdEntities.cs`, `IdGenerators.cs`, `ProtectedFile` tenant-filtered file entity)
- `TestCascadeRelationshipRoles.cs` covers overlapping numeric/plain-string/custom-represented IDs, owner/inverse deletes, one-to-many child cleanup, entity/direct-ID/ID-list/predicate paths, transaction commit/abort, and self-references with repeated initialization.
- `TestReferenceCollectionRoles.cs` covers concurrent opposite-role registrations, repeated subset/superset registrations, and additions of missing flags. It uses a dedicated entity cache so synthetic metadata does not affect cascade integration tests.
- `TestFileIdValidation.cs` verifies uppercase/mixed-case hex rejection before side effects across upload/download/chunk deletion, entity/direct-ID/mixed-list/predicate deletion, and transaction commit/abort. Generated lowercase and non-hex custom file IDs remain supported; `TestFileEntity.cs` verifies public download/delete of legacy BSON ObjectId chunks without migration.
- `TestFileStorageIsolation.cs` covers overlapping custom IDs across file collections (including custom collection names), replacements/reloads, explicit/entity/direct-ID/list/predicate deletion, commit/abort, failed/checksum/cancelled uploads, projected/stale full and partial saves, legacy namespace separation/conflict rejection, deterministic concurrent replacements/deletes, and database isolation. CommandStartedEvent-gated replacement-versus-delete regressions cover entity/direct-ID/list/predicate callers, surviving relationships, mixed-result metadata/chunk/relationship cleanup, and file relationship cascades on transaction commit/abort. Existing global-filter file tests assert new storage-key chunks.
- `streaming_metadata_reads_project_only_storage_fields` monitors download commands with and without a transaction, asserts that only the storage reference and upload-success flag are included (`_id` excluded), and verifies downloaded bytes from an identity-only object when persisted application metadata is present.
- File metadata-copy regressions in that suite cover single/batch saves and inserts with explicit IDs and reset null/empty IDs. Newly persisted metadata must be empty, and deleting, clearing, or uploading to each destination must leave the source byte-for-byte downloadable.
- Gated-upload replacement/deletion races share `ExpectGatedUploadConflict`, which releases the stream and observes the upload before disposal even on entry timeout or competing-action failure. Focused helper tests cover both cleanup success and upload failure without masking the original error. Filtered-out file deletion uses explicit ID/entity data rows with shared metadata/chunk preservation assertions.
- Full-file-save regressions cover single/batch checksum and mapped nullable/default/conditional field removal, preservation of existing upload references/statistics/bytes, successful uploads after clearing the checksum, unchanged partial-save omission behavior, and empty managed metadata on upsert.
- Extra-element full-save regressions cover single/batch deletion and clearing of `[BsonExtraElements]` entries, updated/added values that resemble aggregation expressions, preservation of a newer upload when saving a stale entity, and transaction commit/abort.
- Mapped-file regressions cover explicit inherited-member renames and type-scoped camel-case conventions across entity/direct-ID/list/predicate deletion and transaction commit/abort, plus single/batch full-save upserts with all four managed fields renamed. Relationship concurrency tests use a renamed storage reference. Missing/null mapped references still select only legacy chunks, and legacy ownership conflicts remain protected.
- Explicit chunk-deletion regressions reject non-default cancellation tokens without a session (including already-cancelled tokens) before metadata/chunks change. Transactional clearing with a non-default token verifies cleared metadata/chunks inside the session and restoration of the old reference, statistics, and downloadable bytes after abort.
- Seed legacy file metadata directly through raw collection writes, not `SaveAsync` or library inserts, which intentionally initialize new file identities with empty upload state.
- `FileStorageIsolationTests.batch_legacy_conflict_rejects_all_deletes_before_any_writes` places an uploaded file before a conflicting legacy file in a delete batch and verifies unchanged metadata, chunks, downloadable bytes, and relationships after rejection. All legacy ownership checks must finish before any cascade writes.
- Command-monitored multi-legacy preflight tests verify one collection inventory for both successful deletion and a later candidate conflict, no writes on conflict, and metadata/chunk/relationship preservation or cleanup as appropriate. New-format-only deletion verifies that empty legacy preflight performs no inventory request.
- Extra: `TestFileEntity.cs` (global-filter chunk cascade), `TestMigrations.cs`, `TestMultiClient.cs`, `CappedCollection.cs`, `TestDatabase.cs`
- Fixture migrations: `Tests/Migrations/_001_rename_field.cs`, `_002_undo_field_rename.cs`
- Binary fixture: `Tests/Models/test.jpg` (copy to output; used by file-entity tests). `Tests.csproj` still lists `Models\test.png` but that file is absent and unused.
- Coverage collector: coverlet (package present; no mandated coverage gate in pipeline)

## Commands

`uploads_retry_index_initialization_after_failure` creates a temporary chunk-namespace view to reject index creation, repairs the namespace, then verifies concurrent uploads from two file types succeed with both indexes present. It uses an isolated database and the same client throughout.
```bash
# Requires Mongo reachable (compose default) unless Testcontainers env set
dotnet test Tests/Tests.csproj -c Release

# Testcontainers path (Docker required)
MONGODB_ENTITIES_TESTCONTAINERS=1 dotnet test Tests/Tests.csproj -c Release

# Filter example
dotnet test Tests/Tests.csproj --filter FullyQualifiedName~SavingEntity
```

Azure pipeline: `dotnet test` on `**/*[Tt]ests/*.csproj` with workingDirectory `Tests`, after compose healthy.

## Integration and data
| Mode | When | Connection |
| --- | --- | --- |
| Compose / local | default (env unset) | `mongodb://admin:password@localhost:27017/?replicaSet=rs0&authSource=admin` |
| Testcontainers | `MONGODB_ENTITIES_TESTCONTAINERS` set | `TestDatabase.CreateDatabase()` (image `mongo:8.2`, replica set, ports from 27017++) |

Compose stack (`docker-compose.ci.yml`): `mongo:8.2`, auth, keyfile at `Tests/.mongo-keyfile`, replica set `rs0`. Pipeline generates keyfile (openssl), `chown 999:999`, mode `600`.

Replica set required for transaction tests.

Custom `DB` subclasses in tests (`MyDbEntity`, etc.) exercise global filters and `OnBeforeSave` / `OnBeforeUpdate` hooks.

## Expectations
- New public behavior: add/adjust MSTest methods under `EntityTests` (or sibling test classes); extend `Tests/Models` if new entity shapes needed.
- Prefer real Mongo assertions over mocks.
- `TransactionsEntity` audit-wrapper regressions create and drop isolated databases, pre-create collections, and explicitly supply audit metadata. They cover commit, abort, disposal, session-bound reads/updates/deletes, chained wrappers and filters, disposed-session failures, non-transactional audit views, and new-transaction session ownership.
- `AuditHookTests` uses isolated MongoDB databases to verify persisted exactly-once hooks and per-view audit data across single/batch saves, inserts, partial/preserving saves, replacement, regular/queued updates, and `UpdateAndGet`. It also covers validation rejection, chained/concurrent views, derived overrides, and transactional hook reads with commit/abort/disposal. Run with `--filter 'FullyQualifiedName~AuditHookTests|FullyQualifiedName~TransactionsEntity|FullyQualifiedName~save_preparation_preserves_hook_order_and_upload_ownership'`.
- `SavingEntity` batch-save regressions use single-pass inputs for full and partial saves. Each sets `WithModifiedBy(new UpdatedBy())` explicitly because `BookEntity` requires audit data; do not rely on another test initializing it.
- `FileStorageIsolationTests.save_preparation_preserves_hook_order_and_upload_ownership` supplies audit metadata and checks exactly-once hooks, ID/timestamp visibility, post-hook insert resets (including explicit IDs), and persisted upload preservation for existing-file batch saves. Single/batch `SaveOnlyAsync` and `SaveExceptAsync` cases cover null/empty/generated and explicit/existing IDs, audit persistence, and no in-memory upload reset. Single full saves intentionally retain pre-ID hooks.
- Keep tests sequential-safe; avoid introducing assembly-level parallelization.
- Some existing saving and relationship tests omit required audit metadata. A filtered run can fail with missing `[ModifiedBy]`/`[UpdatedBy]` exceptions. `WithModifiedBy()` returns a new view, not a mutation of `DB.Default`, so callers must use the returned instance. Transaction tests explicitly configure their audit views and can run independently. New regressions should be self-contained.
- Migration tests drop `_migration_history_` when done.

## Sources
- `Tests/Init.cs`
- `Tests/TestDatabase.cs`
- `Tests/Tests.csproj`
- `docker-compose.ci.yml`
- `azure-pipelines.yml`
