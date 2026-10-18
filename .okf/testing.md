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
- Extra: `TestFileEntity.cs` (global-filter chunk cascade), `TestMigrations.cs`, `TestMultiClient.cs`, `CappedCollection.cs`, `TestDatabase.cs`
- Fixture migrations: `Tests/Migrations/_001_rename_field.cs`, `_002_undo_field_rename.cs`
- Binary fixture: `Tests/Models/test.jpg` (copy to output; used by file-entity tests). `Tests.csproj` still lists `Models\test.png` but that file is absent and unused.
- Coverage collector: coverlet (package present; no mandated coverage gate in pipeline)

## Commands
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
- `SavingEntity` batch-save regressions use single-pass inputs for full and partial saves. Each sets `WithModifiedBy(new UpdatedBy())` explicitly because `BookEntity` requires audit data; do not rely on another test initializing it.
- Keep tests sequential-safe; avoid introducing assembly-level parallelization.
- Some existing relationship tests omit required audit metadata. A filtered run can fail with missing `[ModifiedBy]`/`[UpdatedBy]` exceptions. `WithModifiedBy()` returns a new view, not a mutation of `DB.Default`, so callers must use the returned instance. Transaction tests explicitly configure their audit views and can run independently. New regressions should be self-contained.
- Migration tests drop `_migration_history_` when done.

## Sources
- `Tests/Init.cs`
- `Tests/TestDatabase.cs`
- `Tests/Tests.csproj`
- `docker-compose.ci.yml`
- `azure-pipelines.yml`
