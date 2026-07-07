---
type: Reference
title: Dependencies
description: Runtime targets, package managers, key libraries, and compatibility constraints.
tags: [dependencies, dotnet, packages]
---

# Dependencies

## Projects and target frameworks

| Project | Target | Purpose |
| --- | --- | --- |
| `MongoDB.Entities/MongoDB.Entities.csproj` | `netstandard2.1` | NuGet library package. |
| `Tests/Tests.csproj` | `net10.0` | MSTest integration/unit tests. |
| `Benchmark/Benchmark.csproj` | `net10.0` | BenchmarkDotNet console app. |

Library C# language version is pinned to `13` because `MongoDB.Entities.csproj` notes DocFX does not yet support C# 14.

## Package management

- Standard SDK-style .NET projects with inline `PackageReference` entries.
- No central package management file is present.
- NuGet package metadata and version live in `MongoDB.Entities/MongoDB.Entities.csproj`.

## Runtime library dependencies

| Package | Version | Notes |
| --- | --- | --- |
| `MongoDB.Driver` | `3.8.1` | Core MongoDB driver dependency. |
| `SharpCompress` | `0.48.1` | Temporary package reference with project comment: remove when MongoDB driver upgrades to a non-vulnerable version. |
| `Microsoft.SourceLink.GitHub` | `10.0.300` | PrivateAssets source link package. |

## Test dependencies

| Package | Version | Notes |
| --- | --- | --- |
| `Microsoft.NET.Test.Sdk` | `18.5.1` | Test SDK. |
| `MSTest.TestAdapter` | `4.2.3` | MSTest adapter. |
| `MSTest.TestFramework` | `4.2.3` | MSTest framework. |
| `Testcontainers.MongoDb` | `4.11.0` | MongoDB containers for integration tests. |
| `coverlet.collector` | `10.0.0` | Coverage collection; private assets. |

## Benchmark dependencies

| Package | Version | Notes |
| --- | --- | --- |
| `BenchmarkDotNet` | `0.15.8` | Benchmark runner. |
| `Testcontainers.MongoDb` | `4.11.0` | MongoDB for benchmarks. |

## External tools and services

- Docker / Docker Compose for local MongoDB replica-set tests.
- MongoDB 7.0 Docker image for compose and Testcontainers flows.
- DocFX CLI for documentation generation.
- GitHub Actions and Azure Pipelines for release/docs/test automation.

## Update rules

- Changing library dependencies affects the published NuGet package; update docs/OKF if runtime requirements or behavior change.
- Keep `netstandard2.1` support unless explicitly changing package compatibility.
- Check `SharpCompress` comment before removing or changing it; it exists as a temporary mitigation tied to the MongoDB driver dependency.
- When changing package version or release metadata, update `changelog.md` when relevant.

## Sources

- `MongoDB.Entities/MongoDB.Entities.csproj`
- `Tests/Tests.csproj`
- `Benchmark/Benchmark.csproj`
- `docker-compose.ci.yml`
- `.github/workflows/publish-gh-pages.yml`
- `.github/workflows/publish-to-nuget.yml`
