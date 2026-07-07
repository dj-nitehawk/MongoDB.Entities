---
type: Reference
title: Code Map
description: Repository layout, important source areas, generated outputs, and edit guidance.
tags: [code-map, navigation]
---

# Code Map

## Top-level layout

| Path | Purpose |
| --- | --- |
| `MongoDB.Entities.slnx` | Solution file listing library, tests, benchmark, and key repo files. |
| `MongoDB.Entities/` | Main library source and package project. |
| `Tests/` | MSTest project and test models/migrations. |
| `Benchmark/` | BenchmarkDotNet console project. |
| `Documentation/` | DocFX documentation source, API metadata, generated site output, images, and templates. |
| `Artwork/` | Package/docs image assets such as NuGet icon. |
| `.github/workflows/` | GitHub Pages docs publishing and NuGet release workflows. |
| `azure-pipelines.yml` | Tag-triggered CI test pipeline. |
| `docker-compose.ci.yml` | MongoDB 7.0 replica-set service for local/CI tests. |
| `.editorconfig` | Formatting, line ending, naming, and analyzer/ReSharper style rules. |
| `changelog.md` | Release notes body used by GitHub release workflow. |

## Library source map

| Path | Purpose |
| --- | --- |
| `MongoDB.Entities/MongoDB.Entities.csproj` | Package metadata, target framework, nullable setting, and package references. |
| `MongoDB.Entities/DB/DB.cs` | Core `DB` initialization, default instance, client cache, service provider, migration activator, and event hooks. |
| `MongoDB.Entities/DB/DB.*.cs` | Partial `DB` API by capability: CRUD, query, indexes, migrations, files, transactions, watchers, global filters, etc. |
| `MongoDB.Entities/Builders/` | Fluent builders used by `DB` methods and extensions. |
| `MongoDB.Entities/Core/Entities/` | Base entity types (`Entity`, `ObjectIdEntity`, `FileEntity<T>`, `SequenceCounter`). |
| `MongoDB.Entities/Core/Attributes/` | Attributes for collection names, field names, ID handling, preservation, refs, and ignore behavior. |
| `MongoDB.Entities/Core/Interfaces/` | Public contracts such as `IEntity`, `ICreatedOn`, and `IModifiedOn`. |
| `MongoDB.Entities/Core/Serializers/` | Custom BSON serializers. |
| `MongoDB.Entities/Core/Utilities/` | Helpers for fuzzy strings, property paths, templates, and distance algorithms. |
| `MongoDB.Entities/Core/Transaction.cs` | Transaction wrapper and session commit/abort/dispose behavior. |
| `MongoDB.Entities/Core/Watcher.cs` | Change-stream watcher support. |
| `MongoDB.Entities/Extensions/` | Extension methods grouped by feature area. |
| `MongoDB.Entities/Relationships/` | `One`, `Many`, join record, and relationship add/remove/query logic. |
| `MongoDB.Entities/Migrations/` | `IMigration` contract and migration metadata model. |
| `MongoDB.Entities/mongod.cfg` | MongoDB config file kept with library source; check usage before editing. |

## Test map

| Path | Purpose |
| --- | --- |
| `Tests/Tests.csproj` | MSTest project targeting `net10.0`; references library and Testcontainers. |
| `Tests/Init.cs` | Assembly-level test initialization, BSON serializer registration, and MongoDB connection selection. |
| `Tests/TestDatabase.cs` | Testcontainers MongoDB helper. |
| `Tests/EntityTests/` | Feature-focused tests for CRUD, relationships, indexes, transactions, watchers, etc. |
| `Tests/Models/` | Test entity models and sample image files copied to output. |
| `Tests/Migrations/` | Test migration classes. |
| `Tests/Test*.cs`, `Tests/CappedCollection.cs` | Additional integration/feature tests. |

## Documentation map

| Path | Purpose |
| --- | --- |
| `Documentation/wiki/` | Hand-authored user documentation pages. |
| `Documentation/wiki/index.md` | Documentation landing page and feature list. |
| `Documentation/toc.yml` | DocFX top-level navigation. |
| `Documentation/docfx.json` | DocFX metadata/build configuration. |
| `Documentation/api/` | DocFX-generated API YAML metadata from `MongoDB.Entities.csproj`. |
| `Documentation/_site/` | DocFX-generated static site output. |
| `Documentation/templates/`, `Documentation/images/` | Docs theme customizations and images. |

## Generated and caution areas

- Treat `Documentation/api/` and `Documentation/_site/` as generated DocFX outputs; prefer editing source code/XML docs or `Documentation/wiki/` and regenerating docs.
- Treat `bin/`, `obj/`, `TestResults/`, and `Benchmark/BenchmarkDotNet.Artifacts/` as build/test outputs.
- Do not commit secrets. Test service credentials in repo files are local/CI fixtures, not production secrets.
- `Tests/.mongo-keyfile` is created by CI/local setup for MongoDB replica-set auth; do not document or preserve generated key contents.

## Sources

- `MongoDB.Entities.slnx`
- `MongoDB.Entities/MongoDB.Entities.csproj`
- `Tests/Tests.csproj`
- `Benchmark/Benchmark.csproj`
- `Documentation/docfx.json`
- `Documentation/toc.yml`
- `azure-pipelines.yml`
- `docker-compose.ci.yml`
- `.github/workflows/publish-gh-pages.yml`
- `.github/workflows/publish-to-nuget.yml`
