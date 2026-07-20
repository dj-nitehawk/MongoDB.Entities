---
type: Playbook
title: Operations
description: CI, local Mongo services, packaging, and docs deployment surfaces.
tags: [ops]
---

# Operations

## Deploy model
- **Library:** NuGet.org via GitHub Actions on tag `v*` (`publish-to-nuget.yml`).
- **Docs site:** GitHub Pages via `publish-gh-pages.yml` on tag `doc-*` (DocFX → `Documentation/_site`).
- **Tests CI:** Azure DevOps `azure-pipelines.yml` on tag `v*` (not branch builds).

This repo does not deploy a long-running app service.

## Services and ports
| Service | Port | Notes |
| --- | --- | --- |
| MongoDB (compose CI) | `27017` | `mongo:8.2`, replSet `rs0`, auth on |
| Testcontainers Mongo | host ports from 27017 upward | Started per `TestDatabase` |

Compose services: `mongodb`, `mongodb-init` (replica initiate). Volume `mongodb-data`.

## Data stores
- Only MongoDB. Test DB name commonly `mongodb-entities-test`.
- Migration history collection: `_migration_history_`.
- File chunks collection name: `[BINARY_CHUNKS]` (via `[Collection]` on internal type).

## Config and observability
- Connection/auth entirely via driver settings / connection string (no library env config file).
- CI secrets: `NUGET_API_KEY` (Actions). Compose test credentials are local/CI fixtures only; never treat as production.
- Keyfile path for compose: `Tests/.mongo-keyfile` (generated in pipeline; local setup must match permissions for container user).
- No first-class metrics/tracing hooks in the library beyond what the driver exposes.

## Caveats
- Tag-only CI: pushing to a branch does not run the Azure test pipeline as configured.
- NuGet publish and Azure tests both react to `v*` tags; coordinate version/changelog before tagging.
- Docs toolchain SDK (8.x in workflow) may lag main SDK (10.x).

## Sources
- `azure-pipelines.yml`
- `docker-compose.ci.yml`
- `.github/workflows/publish-to-nuget.yml`
- `.github/workflows/publish-gh-pages.yml`
