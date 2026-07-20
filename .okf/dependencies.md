---
type: Reference
title: Dependencies
description: Runtimes, package manager, and key NuGet dependencies.
tags: [deps]
---

# Dependencies

## Runtime
| Project | TFM |
| --- | --- |
| `MongoDB.Entities` | `netstandard2.1` |
| `Tests`, `Benchmark` | `net10.0` |

- SDK: **10.x** for build/test/pack; docs workflow uses **8.x** for DocFX currently.
- Language: C# 13 on library (`LangVersion`).

## Packages
- Package manager: NuGet / `PackageReference` in csproj files.
- Solution: `MongoDB.Entities.slnx`.
- Library pack id: `MongoDB.Entities` (MIT); symbols `snupkg`; SourceLink GitHub.

## Key libraries
| Package | Where | Role |
| --- | --- | --- |
| `MongoDB.Driver` | library | Official driver (version pinned in csproj; upgrade carefully) |
| `SharpCompress` | library | Explicit pin (comment: remove when driver ships non-vulnerable transitive) |
| `Microsoft.SourceLink.GitHub` | library (private) | Source link |
| `MSTest.*`, `Microsoft.NET.Test.Sdk` | Tests | Test host |
| `Testcontainers.MongoDb` | Tests, Benchmark | Optional containerized Mongo |
| `coverlet.collector` | Tests | Coverage |
| `BenchmarkDotNet` | Benchmark | Perf harness |

## Constraints
- Keep library TFM at `netstandard2.1` unless a deliberate breaking platform change.
- Driver upgrades: run full test suite (transactions, change streams, relationships).
- Do not add heavy framework dependencies to the library (ASP.NET, EF, etc.); DI is optional `IServiceProvider` only.
- Version single source for NuGet: `MongoDB.Entities.csproj` `<Version>`.

## Sources
- `MongoDB.Entities/MongoDB.Entities.csproj`
- `Tests/Tests.csproj`
- `Benchmark/Benchmark.csproj`
