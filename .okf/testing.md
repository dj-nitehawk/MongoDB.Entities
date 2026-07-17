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
- Extra: `TestFileEntity.cs` (global-filter chunk cascade), `TestMigrations.cs`, `TestMultiClient.cs`, `CappedCollection.cs`, `TestDatabase.cs`
- Fixture migrations: `Tests/Migrations/_001_rename_field.cs`, `_002_undo_field_rename.cs`
- Binary fixtures: `Tests/Models/test.jpg`, `test.png` (copy to output)
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
| Testcontainers | `MONGODB_ENTITIES_TESTCONTAINERS` set | `TestDatabase.CreateDatabase()` — image `mongo:8.2`, replica set, ports from 27017++ |

Compose stack (`docker-compose.ci.yml`): `mongo:8.2`, auth, keyfile at `Tests/.mongo-keyfile`, replica set `rs0`. Pipeline generates keyfile (openssl), `chown 999:999`, mode `600`.

Replica set required for transaction tests.

Custom `DB` subclasses in tests (`MyDbEntity`, etc.) exercise global filters and `OnBeforeSave` / `OnBeforeUpdate` hooks.

## Expectations
- New public behavior: add/adjust MSTest methods under `EntityTests` (or sibling test classes); extend `Tests/Models` if new entity shapes needed.
- Prefer real Mongo assertions over mocks.
- Keep tests sequential-safe; avoid introducing assembly-level parallelization.
- Migration tests drop `_migration_history_` when done.

## Sources
- `Tests/Init.cs`
- `Tests/TestDatabase.cs`
- `Tests/Tests.csproj`
- `docker-compose.ci.yml`
- `azure-pipelines.yml`
