---
type: Playbook
title: Testing
description: MongoDB integration setup, focused regression suites, and isolation rules.
tags: [test]
---

# Testing

## Setup and commands

MSTest + `Microsoft.NET.Test.Sdk`, targeting `net10.0`; coverlet is available, with no CI coverage threshold. Tests use real MongoDB 8.2. Assembly initialization registers Guid serializers and ID generators, then initializes `mongodb-entities-test`.

```bash
# Default: local replica set from docker-compose.ci.yml
dotnet test Tests/Tests.csproj -c Release

# Alternative: Docker-backed Testcontainers (any set value enables it)
MONGODB_ENTITIES_TESTCONTAINERS=1 dotnet test Tests/Tests.csproj -c Release

# Focused example; use class/filter names from the table below
dotnet test Tests/Tests.csproj -c Release --filter FullyQualifiedName~GlobalFilterCopyTests
```

| Mode | Configuration |
| --- | --- |
| Local/compose (default) | `localhost:27017`, replica set `rs0`, fixture auth from `Tests/Init.cs` / compose |
| Testcontainers | `Tests/TestDatabase.cs`, `mongo:8.2`, replica set, host ports from 27017 upward |

Transactions require a replica set. Compose keyfile preparation and primary readiness checks are in `azure-pipelines.yml`; see [environment traps](gotchas.md#local-mongo-and-tooling). Azure runs the test project after compose startup.

## Find the relevant suite

Feature tests normally live in `Tests/EntityTests/Test*.cs`; shared entities in `Tests/Models/`, migration fixtures in `Tests/Migrations/`. Some specialized suites are directly under `Tests/`.

All paths below are relative to `Tests/`; parenthesized names can be used as `FullyQualifiedName~` filters.

| Surface | Test file(s) or filter |
| --- | --- |
| Saves and partial-write mappings | `EntityTests/TestSaving.cs` (`SavingEntity`), `EntityTests/TestMappedUpdates.cs` (`MappedUpdates`) |
| Copied/view global filters | `EntityTests/TestGlobalFilterCopies.cs` (`GlobalFilterCopyTests`) |
| Audit views, derived hooks, session ownership | `EntityTests/TestAuditHooks.cs` (`AuditHookTests`), `EntityTests/TestTransactions.cs` (`TransactionsEntity`) |
| Pipeline hooks | `EntityTests/TestPipelineHooks.cs` (`PipelineHookTests`) |
| Filtered cascade races and relationship-free fast path | `EntityTests/TestFilteredDeleteRaces.cs` (`FilteredDeleteRaces`) |
| Relationship roles and cache registration | `EntityTests/TestCascadeRelationshipRoles.cs`, `EntityTests/TestReferenceCollectionRoles.cs` |
| ID representation and generation | `EntityTests/TestDynamicIdRelationships.cs`, `EntityTests/TestEntityClassMap.cs`, `EntityTests/TestIdGeneratorResolution.cs`, `EntityTests/TestSerializerRegistration.cs` |
| File ownership, mapping, saves, streaming, races, indexes | `TestFileStorageIsolation.cs` (`FileStorageIsolationTests`) |
| File ID rejection and legacy readability | `TestFileIdValidation.cs` (`FileIdValidationTests`), `TestFileEntity.cs` |
| Database/client routing, watchers, migrations | `EntityTests/TestMultiDb.cs`, `TestMultiClient.cs`, `EntityTests/TestDefaultDbChange.cs`, `EntityTests/TestWatcher.cs`, `TestMigrations.cs` |

Storage/mapping/session contracts live in [Architecture](architecture.md), not an assertion inventory here.

## Isolation and regression design

- `[assembly: DoNotParallelize]` in `Tests/Init.cs` protects shared DB/cache state. Do not enable assembly parallelism without redesigning isolation.
- New behavior needs real persisted-state assertions, including raw BSON for mapping/representation changes and affected consumers for shared helpers. Exercise caller-owned transaction commit/abort when session behavior matters.
- Make focused runs self-contained: explicitly pass required audit metadata and use the instance returned by `WithModifiedBy()`. It does not mutate `DB.Default`. Some older save/relationship tests omit this setup and can fail independently with missing audit-data errors.
- Audit/filter/race regressions use isolated databases; pre-create collections before transactions. Keep synthetic relationship-registry tests on dedicated entity caches so metadata cannot contaminate integration tests. Migration tests clean `_migration_history_`.
- Gate races with command events or controlled streams, not sleeps. Always release gates and observe upload tasks before disposal, including timeout/action-failure paths; `ExpectGatedUploadConflict` is the file-suite pattern. Verify surviving metadata, bytes, and joins, not only delete counts.
- Seed legacy file metadata using raw collection writes, not library saves/inserts that reset new identities. Reuse `Tests/Models/test.jpg` (copied to output). The csproj's unused `test.png` entry refers to an absent file.
- Tests deliberately replace namespaces with views to force index/cleanup failures; keep those cases isolated and restore/drop their databases.

## Sources

- `Tests/Init.cs`
- `Tests/TestDatabase.cs`
- `Tests/Tests.csproj`
- `Tests/EntityTests/` and `Tests/TestFileStorageIsolation.cs`
- `azure-pipelines.yml`
