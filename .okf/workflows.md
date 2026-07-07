---
type: Playbook
title: Workflows
description: Build, test, documentation, benchmark, package, and release commands.
tags: [workflows, commands]
---

# Workflows

## Prerequisites

- .NET SDK 10.x for tests, benchmark, packaging, and CI release workflows.
- .NET SDK 8.x is used by the GitHub Pages docs workflow before installing/running DocFX.
- Docker is needed for the `docker-compose.ci.yml` MongoDB service and for Testcontainers-based test runs.
- DocFX CLI is needed to build documentation locally.

## Restore and build

```bash
dotnet restore MongoDB.Entities.slnx
dotnet build MongoDB.Entities.slnx -c Release
```

For library-only work:

```bash
dotnet build MongoDB.Entities/MongoDB.Entities.csproj -c Release
```

## Test with local docker-compose MongoDB

The CI pipeline creates `Tests/.mongo-keyfile`, starts the MongoDB replica-set service, waits for it to become primary, then runs tests.

```bash
mkdir -p Tests
openssl rand -base64 756 > Tests/.mongo-keyfile
chmod 600 Tests/.mongo-keyfile
docker compose -f docker-compose.ci.yml up -d
# Wait until MongoDB is ready and rs0 has a writable primary.
dotnet test Tests/Tests.csproj -c Release
docker compose -f docker-compose.ci.yml down -v
rm -f Tests/.mongo-keyfile
```

Do not copy the local test credential values from the compose/test files into new documentation. Point readers to `docker-compose.ci.yml` and `Tests/Init.cs` if exact fixture values matter.

## Test with Testcontainers

`Tests/Init.cs` switches to Testcontainers when `MONGODB_ENTITIES_TESTCONTAINERS` is set.

```bash
MONGODB_ENTITIES_TESTCONTAINERS=1 dotnet test Tests/Tests.csproj -c Release
```

Target individual tests with normal MSTest filters, for example:

```bash
dotnet test Tests/Tests.csproj -c Release --filter FullyQualifiedName~TestSaving
```

## Documentation

Build the docs site from DocFX configuration:

```bash
dotnet tool update -g docfx
docfx Documentation/docfx.json
```

- Hand-authored docs: `Documentation/wiki/`.
- Generated API metadata: `Documentation/api/`.
- Generated static site: `Documentation/_site/`.
- GitHub Pages publishes on `doc-*` tags.

## Benchmark

```bash
dotnet run --project Benchmark/Benchmark.csproj -c Release
```

`Benchmark/run.cmd` contains the Windows form of the same command.

## Package and release

Create NuGet packages:

```bash
dotnet pack MongoDB.Entities/MongoDB.Entities.csproj -c Release
```

Release automation:

- GitHub NuGet workflow runs on `v*` tags, packs the library, pushes to NuGet using the repository secret, and creates a GitHub release using `changelog.md`.
- Azure Pipelines also runs tests on `v*` tags.
- Documentation publishing runs on `doc-*` tags.

## Common edit workflow

1. Read relevant OKF and source/docs files.
2. Make focused source, test, or docs changes.
3. Run the narrowest reliable validation first, then broader validation when practical.
4. Update `Documentation/wiki/` and generated docs if public API/documented behavior changes.
5. Update `.okf/` if commands, architecture, dependencies, tests, operations, or conventions changed.

## Sources

- `MongoDB.Entities.slnx`
- `MongoDB.Entities/MongoDB.Entities.csproj`
- `Tests/Tests.csproj`
- `Benchmark/Benchmark.csproj`
- `Benchmark/run.cmd`
- `azure-pipelines.yml`
- `docker-compose.ci.yml`
- `.github/workflows/publish-gh-pages.yml`
- `.github/workflows/publish-to-nuget.yml`
- `Documentation/docfx.json`
