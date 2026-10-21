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
- Language: C# 13 on library (`LangVersion`), deliberately held back because of DocFX C# 14 support lag noted in the csproj.

## Packages
- Package manager: NuGet / `PackageReference` in csproj files.
- Read package versions from the project files below, not copied snapshots.
- The explicit library `SharpCompress` pin is a vulnerability mitigation; remove only when the driver supplies a non-vulnerable transitive version.

## Constraints
- Keep library TFM at `netstandard2.1` unless a deliberate breaking platform change.
- Driver upgrades: run full test suite (transactions, change streams, relationships).
- Do not add heavy framework dependencies to the library (ASP.NET, EF, etc.); DI is optional `IServiceProvider` only.
- Version single source for NuGet: `MongoDB.Entities.csproj` `<Version>`.

## Sources
- `MongoDB.Entities/MongoDB.Entities.csproj`
- `Tests/Tests.csproj`
- `Benchmark/Benchmark.csproj`
