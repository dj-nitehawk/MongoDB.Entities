---
type: Reference
title: Operations
description: CI, release, docs publishing, local services, and operational caveats.
tags: [operations, ci, release]
---

# Operations

## Runtime deployment model

MongoDB.Entities is a library package, not a hosted service. Runtime operations are mostly consumer-owned: applications initialize MongoDB connections with `DB.InitAsync`, pass driver settings/connection strings, and manage their own MongoDB server authentication/authorization.

## CI test pipeline

`azure-pipelines.yml` runs only for `v*` tags:

1. Creates a local `Tests/.mongo-keyfile` for MongoDB replica-set auth.
2. Starts services from `docker-compose.ci.yml`.
3. Waits for MongoDB to respond and become writable primary.
4. Installs .NET SDK 10.x.
5. Runs `dotnet test` for test projects in Release configuration.
6. Tears down Docker services and deletes the keyfile in an `always()` step.

## Local/CI MongoDB service

`docker-compose.ci.yml` defines:

- `mongodb`: MongoDB 7.0 with replica set `rs0`, auth, port `27017`, and keyfile mount.
- `mongodb-init`: one-shot replica-set initiation container.
- `mongodb-data`: named Docker volume.

Do not publish or copy fixture credential values into new docs. Reference the compose/test files when exact local fixture settings are needed.

## Testcontainers mode

Set `MONGODB_ENTITIES_TESTCONTAINERS` to make tests create MongoDB containers via `Tests/TestDatabase.cs`. This path avoids requiring the compose service but still requires Docker.

## NuGet release

`.github/workflows/publish-to-nuget.yml` runs on `v*` tags:

- checks out source;
- installs .NET SDK 10.x;
- runs `dotnet pack MongoDB.Entities/MongoDB.Entities.csproj -c Release`;
- pushes `.nupkg` files from `MongoDB.Entities/bin/Release/` using a repository secret;
- creates a GitHub release from `changelog.md` unless the tag contains `beta`.

## Documentation publishing

`.github/workflows/publish-gh-pages.yml` runs on `doc-*` tags:

- installs .NET SDK 8.x;
- installs/updates DocFX as a global tool;
- runs `docfx Documentation/docfx.json`;
- publishes `Documentation/_site` to GitHub Pages.

`Documentation/docfx.json` generates API metadata from `MongoDB.Entities/MongoDB.Entities.csproj` and includes wiki pages, API YAML, images, and templates.

## Observability

The library exposes change-stream watcher support, but this repo has no hosted observability stack. Consumers supply logging/monitoring at application level.

## Operational caveats

- Transactions and change streams require replica-set-capable MongoDB; standalone MongoDB is not enough for full test coverage.
- Tests and benchmarks may create containers or use port 27017; avoid running multiple conflicting local services.
- Generated docs and build outputs can be large; edit sources and regenerate rather than hand-editing generated output.

## Sources

- `azure-pipelines.yml`
- `docker-compose.ci.yml`
- `.github/workflows/publish-to-nuget.yml`
- `.github/workflows/publish-gh-pages.yml`
- `Documentation/docfx.json`
- `Tests/Init.cs`
- `Tests/TestDatabase.cs`
