---
type: Reference
title: Code Map
description: Top-level layout and where to add library features, tests, and docs.
tags: [layout]
---

# Code Map

## Layout
| Path | Purpose |
| --- | --- |
| `MongoDB.Entities/` | Packable library (NuGet `MongoDB.Entities`) |
| `MongoDB.Entities/DB/` | `DB` partials: one concern per file (`DB.Save.cs`, `DB.Find.cs`, …) |
| `MongoDB.Entities/Builders/` | Fluent builders (`Find`, `Update`, `Replace`, `Index`, …) |
| `MongoDB.Entities/Core/` | Entities, attributes, serializers, `Transaction`, `Watcher`, `Template`, cache |
| `MongoDB.Entities/Relationships/` | `One`, `Many.*`, `JoinRecord` |
| `MongoDB.Entities/Extensions/` | Extension methods for entities/collections/relationships |
| `MongoDB.Entities/Migrations/` | `IMigration`, `Migration` history entity |
| `Tests/` | MSTest project; entity tests, models, migrations fixtures |
| `Tests/EntityTests/` | Feature tests (saving, relationships, watcher, …) |
| `Tests/Models/` | Shared test entities (`BookEntity`, `AuthorEntity`, …) |
| `Tests/Migrations/` | Sample `_001_…` / `_002_…` migrations for tests |
| `Benchmark/` | BenchmarkDotNet entry (`Program.cs`) + benchmarks |
| `Documentation/` | DocFX (`docfx.json`, `wiki/`, `api/`, `_site/`) |
| `Artwork/` | Package icon |
| `azure-pipelines.yml` | Tag-triggered Azure DevOps test pipeline |
| `docker-compose.ci.yml` | MongoDB 8.2 replica set for CI/local tests |
| `.github/workflows/` | NuGet publish + GitHub Pages docs |

Solution file: `MongoDB.Entities.slnx` (projects: library, Tests, Benchmark).

## Modules (library)
- **Init / multi-DB:** `DB/DB.cs`: `InitAsync`, `Default`, `Instance`, `ChangeDefaultDatabase`
- **ID generators:** `DB/DB.IdGenerators.cs`: `RegisterIdGenerator<T>`; resolution/helpers in `Extensions/Identity.cs` + `Core/Cache.cs`
- **CRUD:** `DB.Save`, `DB.Insert`, `DB.Delete`, `DB.Replace`, `DB.Update` / `UpdateAndGet` + matching builders
- **Query:** `DB.Find`, `DB.Queryable`, `DB.PagedSearch`, `DB.Distinct`, `DB.Count`, `DB.Pipeline`, `DB.Fluent`, `DB.GeoNear`
- **Meta:** `DB.Collection`, `DB.Index`, `DB.Sequence`, `DB.GlobalFilters`, `DB.File`, `DB.Watcher`, `DB.Transaction`, `DB.Migrate`
- **Entity base:** `Core/Entities/Entity.cs`, `FileEntity.cs`, `ObjectIdEntity.cs`
- **Binary storage:** `Core/Entities/FileEntity.cs` (streaming/metadata), `Core/FileMetadata.cs` (managed-property policy and save updates), `Core/FileStorage.cs` (chunk/cascade I/O, legacy preflight, indexes). [Architecture](architecture.md#file-storage) owns the persistence contracts.
- **Contracts:** `Core/Interfaces/IEntity.cs`, `ICreatedOn`, `IModifiedOn`

## Entry points
| Concern | Start here |
| --- | --- |
| New DB operation | `MongoDB.Entities/DB/DB.<Area>.cs` + builder if fluent |
| Entity / attribute | `MongoDB.Entities/Core/` |
| Relationship behavior | `MongoDB.Entities/Relationships/` + `Extensions/Relationships.cs` |
| Migration framework | `DB.Migrate.cs`, `Migrations/` |
| Test for feature | `Tests/EntityTests/Test*.cs` + models under `Tests/Models/` |
| User-facing docs | `Documentation/wiki/*.md` |
| v26 upgrade and data compatibility | `Documentation/wiki/Upgrade-to-v26.md` (linked from the wiki TOC) |
| Package version / deps | `MongoDB.Entities/MongoDB.Entities.csproj` |

## Generated code
- DocFX output: `Documentation/_site/`, `Documentation/api/`; regenerate; do not hand-edit as source of truth.
- No source generators in the library project.

## Sources
- `MongoDB.Entities.slnx`
- Directory layout under `MongoDB.Entities/`, `Tests/`, `Documentation/`
