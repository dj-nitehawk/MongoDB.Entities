---
type: Reference
title: Testing
description: Test framework, layout, MongoDB dependencies, and validation expectations.
tags: [testing, mstest, mongodb]
---

# Testing

## Framework and target

- Test project: `Tests/Tests.csproj`.
- Framework: MSTest (`MSTest.TestFramework`, `MSTest.TestAdapter`) with `Microsoft.NET.Test.Sdk`.
- Target framework: `net10.0`.
- Coverage collector: `coverlet.collector`.
- Integration database options: local MongoDB from `docker-compose.ci.yml` or Testcontainers MongoDB.

## Initialization

`Tests/Init.cs` runs once for the assembly:

- registers a standard `GuidSerializer`;
- checks `MONGODB_ENTITIES_TESTCONTAINERS`;
- if set, starts two MongoDB Testcontainers instances;
- otherwise connects to the local replica-set service on port 27017;
- initializes the `mongodb-entities-test` database through `DB.InitAsync`.

## Test layout

| Path | Purpose |
| --- | --- |
| `Tests/EntityTests/` | Main feature suites: counting, date, default DB, delete, distinct, fuzzy string, geospatial, indexes, modified-by, multi-db, paging, props, relationships, replace, save, sort, tag replacement, transactions, update/update-and-get, watcher. |
| `Tests/Models/` | Entity models and sample images used by tests. |
| `Tests/Migrations/` | Migration classes used by migration tests. |
| `Tests/TestDatabase.cs` | Testcontainers MongoDB builder/helper. |
| `Tests/TestMigrations.cs` | Migration-system tests. |
| `Tests/TestMultiClient.cs` | Multi-client tests. |
| `Tests/TestFileEntity.cs` | File storage tests. |
| `Tests/CappedCollection.cs` | Capped collection tests. |

## Commands

Run all tests with local docker-compose MongoDB after creating the keyfile and starting the service:

```bash
dotnet test Tests/Tests.csproj -c Release
```

Run all tests with Testcontainers:

```bash
MONGODB_ENTITIES_TESTCONTAINERS=1 dotnet test Tests/Tests.csproj -c Release
```

Run targeted tests with MSTest filters:

```bash
dotnet test Tests/Tests.csproj -c Release --filter FullyQualifiedName~TestRelationships
```

## Database dependencies

- Tests need a MongoDB replica set because transactions and change streams require replica-set behavior.
- `docker-compose.ci.yml` defines a MongoDB 7.0 service, a replica-set init container, and persistent test volume.
- CI creates and removes `Tests/.mongo-keyfile` around the run. Treat it as generated local test material.

## What to test when changing behavior

- Public CRUD/query/builder changes: add or update a focused test under `Tests/EntityTests/`.
- Relationship behavior: update `TestRelationships` and relevant model classes.
- Migrations: update `Tests/Migrations/` plus `TestMigrations`.
- File storage: update `TestFileEntity` and sample data expectations.
- Initialization/default/multi-client logic: update `TestDefaultDbChange`, `TestMultiDb`, or `TestMultiClient` as applicable.
- Transactions/change streams: validate against a replica-set-backed database, not a standalone MongoDB instance.

## Sources

- `Tests/Tests.csproj`
- `Tests/Init.cs`
- `Tests/TestDatabase.cs`
- `Tests/EntityTests/`
- `Tests/Migrations/`
- `docker-compose.ci.yml`
- `azure-pipelines.yml`
