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
# Creates Tests/.mongo-keyfile first; see testing.md / azure-pipelines.yml
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
- Style enforced via `.editorconfig` + ReSharper/Rider (`.DotSettings` present). No dedicated `dotnet format` CI step observed; match surrounding code and editorconfig.

## Docs
```bash
dotnet tool update -g docfx   # as in GH workflow
docfx Documentation/docfx.json
# output: Documentation/_site
```

Wiki markdown source: `Documentation/wiki/`. Site CNAME / pages deploy on tags `doc-*`.
DocFX validates documentation structure and links, not whether C# snippets compile. When changing examples, compile representative complete snippets against the current library and verify behavioral examples separately. Keep validation output outside tracked generated directories (for example, `docfx build Documentation/docfx.json --output /tmp/mongodb-entities-docs`).

## Codegen and migrations
- No codegen step for the library.
- **User** data migrations: implement `IMigration`, name `_NNN_…`, call `db.MigrateAsync()` / `MigrateAsync<T>()` / `MigrationsAsync(...)` at app startup, not a repo build step.

## Release (observed automation)
| Trigger | Pipeline | Action |
| --- | --- | --- |
| Git tag `v*` | `azure-pipelines.yml` | Start compose Mongo, `dotnet test` |
| Git tag `v*` | `.github/workflows/publish-to-nuget.yml` | `dotnet pack` + nuget push + GH release from `changelog.md` (skip GH release creation for `beta` tags) |
| Git tag `doc-*` | `publish-gh-pages.yml` | DocFX → GitHub Pages |

Bump package version in `MongoDB.Entities/MongoDB.Entities.csproj` (`Version`) and maintain root `changelog.md` for release notes.
Keep release notes reader-focused. The v26 compatibility details, migration examples, and data/reference/file-chunk warnings live in `Documentation/wiki/Upgrade-to-v26.md`. When release notes need that guidance, link to the repository copy so GitHub releases do not depend on a separate docs-site publish.

## Changelog requirement

Root `changelog.md` is rolling current-cycle release notes, not permanent history or a Keep-a-Changelog file. Non-beta tags use the **entire file** as the GitHub release body. Do not add version headers, invent a release version, or bump the package version unless requested. Preserve unrelated entries and any introductory content or banners. Clear entries only when explicitly asked as part of release work; do not rewrite historical release notes.

Update it in the same change as user-visible library work (new public API/package/feature, user-visible bug fix, notable performance/behavior improvement, breaking change). Skip tests-only, OKF, comments, CI, formatting, documentation-only edits, and internal refactors with no consumer effect. State why the changelog is unaffected when omitting an entry. Update an existing entry for the same change instead of duplicating it.

Prepend a new `<details>` immediately under the matching heading (newest first). Use this repository's section names: `## New features`, `## Bug fixes`, `## Improvements`, and `## Breaking changes`. Reuse existing headings; add a missing standard heading only when needed, not a new category. Existing unrelated bullet entries can remain unchanged.

Use HTML `<details><summary>user-facing title</summary>` plus short consumer-facing prose and an optional small code sample:

```html
<details>
<summary>User-facing title</summary>

Short explanation of the practical impact.

</details>
```

**Voice and detail:** Write short, conversational entries in the maintainer's voice, speaking directly to users. Focus on their needs and practical impact, not a technical dump. Summaries state consumer impact, not implementation. Prefer plain language over API inventories. Keep useful code examples, but leave detailed setup, storage contracts, and operational edge cases to public documentation. Add a documentation pointer where appropriate. Retain essential compatibility, safety, and migration warnings. These rules stand on their own; do not rely on temporary changelog entries as tone references.

- **New features:** Introduce the capability, who it helps, and why. A useful shape is: "If you're building [use case], you can now [capability]. [Brief benefit or relevant compatibility note.]" Follow with an optional example and documentation pointer, not a mandatory formula.
- **Bug fixes:** Describe the user-visible problem and what now works correctly.
- **Improvements:** State what is better and the practical benefit.
- **Breaking changes:** Say what changed, who is affected, and what users must do to migrate. Include a small before/after example when useful. Give users enough detail to act safely, while keeping the same conversational voice.

Changelog is release notes. `Documentation/wiki/` and public API XML docs provide detailed usage and API documentation. Update both when both apply.

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
- `changelog.md`
