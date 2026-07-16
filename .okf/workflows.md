---
type: Playbook
title: Workflows
description: Build, test, pack, docs, and release commands for MongoDB.Entities.
tags: [build]
---

# Workflows

## Setup
- .NET SDK **10.x** for library build/tests/pack (CI and GitHub Actions use `10.x`).
- Docs publish workflow currently uses **8.x** + global `docfx` tool (see `.github/workflows/publish-gh-pages.yml`).
- MongoDB for tests: either local compose stack or Testcontainers (see `testing.md`).

```bash
# Local MongoDB 8.2 replica set (matches CI)
# Creates Tests/.mongo-keyfile first — see testing.md / azure-pipelines.yml
docker compose -f docker-compose.ci.yml up -d
```

## Build and run
```bash
dotnet build MongoDB.Entities.slnx -c Release
dotnet build MongoDB.Entities/MongoDB.Entities.csproj -c Release
dotnet pack MongoDB.Entities/MongoDB.Entities.csproj -c Release
```

Benchmark (Release):
```bash
dotnet run --project Benchmark/Benchmark.csproj -c Release
# or Benchmark/run.cmd
```

## Lint and format
- Style enforced via `.editorconfig` + ReSharper/Rider (`.DotSettings` present). No dedicated `dotnet format` CI step observed — match surrounding code and editorconfig.

## Docs
```bash
dotnet tool update -g docfx   # as in GH workflow
docfx Documentation/docfx.json
# output: Documentation/_site
```

Wiki markdown source: `Documentation/wiki/`. Site CNAME / pages deploy on tags `doc-*`.

## Codegen and migrations
- No codegen step for the library.
- **User** data migrations: implement `IMigration`, name `_NNN_…`, call `db.MigrateAsync()` / `MigrateAsync<T>()` / `MigrationsAsync(...)` at app startup — not a repo build step.

## Release (observed automation)
| Trigger | Pipeline | Action |
| --- | --- | --- |
| Git tag `v*` | `azure-pipelines.yml` | Start compose Mongo, `dotnet test` |
| Git tag `v*` | `.github/workflows/publish-to-nuget.yml` | `dotnet pack` + nuget push + GH release from `changelog.md` (skip release body path for `beta` tags) |
| Git tag `doc-*` | `publish-gh-pages.yml` | DocFX → GitHub Pages |

Bump package version in `MongoDB.Entities/MongoDB.Entities.csproj` (`Version`) and maintain root `changelog.md` for release notes.

## Env vars (names only)
| Name | Use |
| --- | --- |
| `MONGODB_ENTITIES_TESTCONTAINERS` | If set (any value), tests use Testcontainers instead of localhost compose |
| `NUGET_API_KEY` | GitHub Actions secret for nuget push (`NUGET_AUTH_TOKEN` env in workflow) |

## Sources
- `azure-pipelines.yml`
- `.github/workflows/publish-to-nuget.yml`
- `.github/workflows/publish-gh-pages.yml`
- `Benchmark/Program.cs`
