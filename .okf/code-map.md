---
type: Reference
title: Code Map
description: Top-level layout and where to add library features, tests, and docs.
tags: [layout]
---

# Code Map

## Where to edit
| Path | Concern |
| --- | --- |
| `MongoDB.Entities/DB/` | Public `DB` operations, one concern per partial (`DB.Save.cs`, `DB.Find.cs`, etc.); initialization/routing in `DB.cs` |
| `MongoDB.Entities/Builders/` | Fluent builders matching DB operations (`Find`, `Update`, `Replace`, `Index`, etc.) |
| `MongoDB.Entities/Core/` | Entity contracts, attributes, serializers, `Transaction`, `Watcher`, `Template`, cache |
| `MongoDB.Entities/Relationships/`, `MongoDB.Entities/Extensions/Relationships.cs` | `One`, `Many.*`, `JoinRecord`, relationship extensions |
| `MongoDB.Entities/Extensions/` | Other entity/collection extensions; `Identity.cs` contains ID helpers |
| `MongoDB.Entities/Migrations/`, `MongoDB.Entities/DB/DB.Migrate.cs` | Migration framework and history |
| `Tests/` | Feature suites, shared `Models/`, migration fixtures in `Migrations/`; see [suite selection](testing.md#find-the-relevant-suite) |
| `Benchmark/` | BenchmarkDotNet entry (`Program.cs`) + benchmarks |
| `Documentation/wiki/` | Public documentation; `Upgrade-to-v26.md` owns migration/data compatibility guidance |
| `Documentation/docfx.json` | DocFX configuration |
| `MongoDB.Entities/MongoDB.Entities.csproj` | Package version, library target and dependencies |
| `azure-pipelines.yml` | Tag-triggered Azure DevOps test pipeline |
| `docker-compose.ci.yml` | Replica set for CI/local tests; see [Testing](testing.md) |
| `.github/workflows/` | NuGet publish + GitHub Pages docs |

Solution file: `MongoDB.Entities.slnx` (projects: library, Tests, Benchmark).

Targeted starting points within the library:
- **ID generation:** `DB/DB.IdGenerators.cs`, `Extensions/Identity.cs`, `Core/Cache.cs`.
- **Entity contracts:** `Core/Entities/`, `Core/Interfaces/`.
- **Binary storage:** `Core/Entities/FileEntity.cs` (streaming/metadata), `Core/FileMetadata.cs` (managed-property/save policy), `Core/FileStorage.cs` (I/O, legacy preflight, indexes). [Architecture](architecture.md#file-storage) owns persistence contracts.

## Generated code
- DocFX output: `Documentation/_site/`, `Documentation/api/`; regenerate; do not hand-edit as source of truth.

## Sources
- `MongoDB.Entities.slnx`
- Directory layout under `MongoDB.Entities/`, `Tests/`, `Documentation/`
