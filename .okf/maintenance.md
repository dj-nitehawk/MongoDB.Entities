---
type: Reference
title: Maintenance
description: How to keep the OKF knowledge set conformant and synchronized.
tags: [maintain]
---

# Maintenance

Day-to-day reading, content discipline, conflict handling, and finish gates: repo [AGENTS.md](../AGENTS.md). This file owns the trigger inventory and conformance detail.

## Conformance
- OKF v0.1: non-reserved `.md` files need YAML frontmatter with non-empty `type` (allowed list), `title`, and `description`.
- Bundle-root `index.md` is a listing; only it may set `okf_version: "0.1"`.
- Allowed types: `Reference`, `Architecture`, `Playbook`, `API Endpoint`, `Database`, `Service`, `Event`, `Security`, `Deployment`, `Generated`, `ADR`. Do not invent types; use `Reference` if unsure.
- No empty placeholders; omit inapplicable files rather than stubbing.
- Optional `log.md` is append-only notes, not a concept file; do not create unless requested.

## Update triggers
Evaluate `.okf/` when changes affect:
- architecture / module boundaries
- public APIs, entity contracts, builders, migrations framework
- persistence / collection / relationship behavior
- dependencies, TFMs, package versions, package manager layout
- build / test / pack / docs / deploy commands or CI
- testing strategy, Mongo fixtures, parallelization
- security/auth connection assumptions
- config/env/ports/ops
- conventions, layout, gotchas
- changelog path/format/voice and GitHub release-note workflow (not individual entry bodies)

## Topic ownership

| Topic | Detailed home |
| --- | --- |
| Runtime boundaries, views/sessions, persistence and write contracts | `architecture.md` |
| Where to edit, generated paths | `code-map.md` |
| Coding/API style | `conventions.md` |
| Build/docs/release commands and changelog policy | `workflows.md` |
| Test setup, suite selection, isolation | `testing.md` |
| Runtime/package compatibility | `dependencies.md` |
| Deployment/CI services and configuration | `operations.md` |
| Identity compatibility, environment traps, safety pointers | `gotchas.md` |

Keep safety constraints at their detailed home; link from other topics rather than repeating implementation or assertion inventories. Read package versions from manifests, not copied snapshots.
