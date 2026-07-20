---
okf_version: "0.1"
---

# OKF Knowledge Set

Compact operational knowledge for agents working on MongoDB.Entities. Read relevant files before editing. Keep synchronized with code, tests, docs, and configuration.

## Core reading order
* [Project Overview](project-overview.md): purpose and scope
* [Architecture](architecture.md): boundaries and invariants
* [Code Map](code-map.md): where things live
* [Conventions](conventions.md): coding/design rules

## Workflow and validation
* [Workflows](workflows.md): build, pack, docs, release
* [Testing](testing.md): MSTest, MongoDB, Testcontainers

## Task-specific
* [Dependencies](dependencies.md) · [Operations](operations.md) · [Gotchas](gotchas.md) · [Maintenance](maintenance.md)

## Authority
If OKF conflicts with source, tests, generated artifacts, or manifests: verify those, then update OKF.

## Maintenance
Normative OKF use/update gates: repo canonical agent instructions (`AGENTS.md`). Reminder + conformance detail: [Maintenance](maintenance.md).
Before finishing, sync OKF when triggers apply; if not needed, state why (`OKF unaffected (non-behavioral edit)` for pure comment/typo/format).
